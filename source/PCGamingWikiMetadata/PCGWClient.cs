using Newtonsoft.Json.Linq;
using PCGamingWikiBulkImport;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PCGamingWikiMetadata;

public class PCGWClient(MetadataRequestOptions options, PCGWGameController gameController)
{
    private readonly ILogger logger = LogManager.GetLogger();
    private readonly RestClient client = new RestClient("https://www.pcgamingwiki.com/w/api.php").AddDefaultQueryParameter("format", "json");
    protected MetadataRequestOptions options = options;
    protected PCGWGameController gameController = gameController;

    public JObject ExecuteRequest(RestRequest request)
    {
        var fullUrl = client.BuildUri(request);
        logger.Info(fullUrl.ToString());

        var response = client.Execute(request);

        if (response.ErrorException != null)
        {
            const string message = "Error retrieving response.  Check inner details for more info.";
            var e = new Exception(message, response.ErrorException);
            throw e;
        }
        var content = response.Content;

        return JObject.Parse(content);
    }

    private string NormalizeSearchString(string search)
    {
        // Replace ' with " as a workaround for search API returning no results
        return search.Replace('-', ' ').Replace('\'', '"');
    }

    public List<GenericItemOption> SearchGames(string searchName)
    {
        List<GenericItemOption> gameResults = [];
        logger.Info(searchName);

        var request = new RestRequest();
        request.AddQueryParameter("action", "query");
        request.AddQueryParameter("list", "search");
        request.AddQueryParameter("srlimit", 300);
        request.AddQueryParameter("srwhat", "title");
        request.AddQueryParameter("srsearch", $"\"{NormalizeSearchString(searchName)}\"");

        try
        {
            JObject searchResults = ExecuteRequest(request);

            if (searchResults.TryGetValue("error", out JToken error))
            {
                logger.Error($"Encountered API error: {error.ToString()}");
                return gameResults;
            }

            logger.Debug($"SearchGames {searchResults["query"]["searchinfo"]["totalhits"]} results for {searchName}");

            foreach (dynamic game in searchResults["query"]["search"])
            {
                PCGWGame g = new(gameController.Settings, (string)game.title, (int)game.pageid);
                gameResults.Add(g);
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Error performing search");
        }

        return gameResults.OrderBy(game => NameStringCompare(searchName, game.Name)).ToList();
    }

    public virtual void FetchGamePageContent(PCGWGame game)
    {
        var request = new RestRequest()
            .AddQueryParameter("action", "parse")
            .AddQueryParameter("page", game.Name.TitleToSlug(urlEncode: false));

        game.LibraryGame = this.options.GameData;

        try
        {
            JObject content = ExecuteRequest(request);

            if (content.TryGetValue("error", out JToken error))
            {
                logger.Error($"Encountered API error: {error.ToString()}");
            }

            PCGamingWikiJSONParser jsonParser = new(content, this.gameController);
            PCGamingWikiHTMLParser parser = new(jsonParser.PageHTMLText(), this.gameController);


            if (parser.CheckPageRedirect(out string redirectPage))
            {
                logger.Debug($"redirect link: {redirectPage}");
                game.Name = redirectPage;
                FetchGamePageContent(game);
            }
            else
            {
                jsonParser.ParseGameDataJson();
                parser.ApplyGameMetadata();
            }
        }
        catch (Exception e)
        {
            logger.Error($"Error performing FetchGamePageContent for {game.Name}: {e}");
        }
    }

    // https://en.wikibooks.org/wiki/Algorithm_Implementation/Strings/Levenshtein_distance#C.23
    private static int NameStringCompare(string a, string b)
    {
        if (string.IsNullOrEmpty(a))
        {
            if (!string.IsNullOrEmpty(b))
            {
                return b.Length;
            }
            return 0;
        }

        if (string.IsNullOrEmpty(b))
        {
            if (!string.IsNullOrEmpty(a))
            {
                return a.Length;
            }
            return 0;
        }

        int cost;
        int[,] d = new int[a.Length + 1, b.Length + 1];
        int min1;
        int min2;
        int min3;

        for (int i = 0; i <= d.GetUpperBound(0); i += 1)
        {
            d[i, 0] = i;
        }

        for (int i = 0; i <= d.GetUpperBound(1); i += 1)
        {
            d[0, i] = i;
        }

        for (int i = 1; i <= d.GetUpperBound(0); i += 1)
        {
            for (int j = 1; j <= d.GetUpperBound(1); j += 1)
            {
                cost = (a[i - 1] != b[j - 1]) ? 1 : 0;

                min1 = d[i - 1, j] + 1;
                min2 = d[i, j - 1] + 1;
                min3 = d[i - 1, j - 1] + cost;
                d[i, j] = Math.Min(Math.Min(min1, min2), min3);
            }
        }

        return d[d.GetUpperBound(0), d.GetUpperBound(1)];
    }
}
