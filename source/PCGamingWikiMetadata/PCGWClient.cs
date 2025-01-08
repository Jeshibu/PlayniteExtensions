using Playnite.SDK.Plugins;
using Playnite.SDK;
using RestSharp;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;

namespace PCGamingWikiMetadata
{
    public class PCGWClient
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly string baseUrl = @"https://www.pcgamingwiki.com/w/api.php";
        private RestClient client;
        protected MetadataRequestOptions options;
        protected PCGWGameController gameController;

        public PCGWClient(MetadataRequestOptions options, PCGWGameController gameController)
        {
            client = new RestClient(baseUrl);
            client.UserAgent = "Playnite";
            client.Encoding = Encoding.UTF8;
            this.options = options;
            this.gameController = gameController;
        }

        public JObject ExecuteRequest(RestRequest request)
        {
            request.AddParameter("format", "json", ParameterType.QueryString);

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
            return search.Replace("-", " ").Replace("'", "\"");
        }

        public List<GenericItemOption> SearchGames(string searchName)
        {
            List<GenericItemOption> gameResults = new List<GenericItemOption>();
            logger.Info(searchName);

            var request = new RestRequest("/", Method.GET);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Accept", "application/json, text/json, text/x-json");

            request.AddParameter("action", "query", ParameterType.QueryString);
            request.AddParameter("list", "search", ParameterType.QueryString);
            request.AddParameter("srlimit", 300, ParameterType.QueryString);
            request.AddParameter("srwhat", "title", ParameterType.QueryString);
            request.AddParameter("srsearch", $"\"{NormalizeSearchString(searchName)}\"", ParameterType.QueryString);

            try
            {
                JObject searchResults = ExecuteRequest(request);
                JToken error;

                if (searchResults.TryGetValue("error", out error))
                {
                    logger.Error($"Encountered API error: {error.ToString()}");
                    return gameResults;
                }

                logger.Debug($"SearchGames {searchResults["query"]["searchinfo"]["totalhits"]} results for {searchName}");

                foreach (dynamic game in searchResults["query"]["search"])
                {
                    if (!((string)game.snippet).Contains("#REDIRECT"))
                    {
                        PCGWGame g = new PCGWGame(gameController.Settings, (string)game.title, (int)game.pageid);
                        gameResults.Add(g);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error performing search");
            }

            return gameResults.OrderBy(game => NameStringCompare(searchName, game.Name)).ToList<GenericItemOption>();
        }

        public virtual void FetchGamePageContent(PCGWGame game)
        {
            var request = new RestRequest("/", Method.GET);
            request.AddParameter("action", "parse", ParameterType.QueryString);
            request.AddParameter("page", game.Name.Replace(" ", "_"), ParameterType.QueryString);

            game.LibraryGame = this.options.GameData;

            try
            {
                JObject content = ExecuteRequest(request);
                JToken error;

                if (content.TryGetValue("error", out error))
                {
                    logger.Error($"Encountered API error: {error.ToString()}");
                }

                PCGamingWikiJSONParser jsonParser = new PCGamingWikiJSONParser(content, this.gameController);
                PCGamingWikiHTMLParser parser = new PCGamingWikiHTMLParser(jsonParser.PageHTMLText(), this.gameController);

                string redirectPage;

                if (parser.CheckPageRedirect(out redirectPage))
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
        private int NameStringCompare(string a, string b)
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
}
