using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCGamingWikiBulkImport;
using PCGamingWikiBulkImport.DataCollection;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PCGamingWikiMetadata;

public class PCGWClient(MetadataRequestOptions options, PCGWGameController gameController, IWebDownloader downloader) : ICargoQuery
{
    private readonly ILogger _logger = LogManager.GetLogger();
    private const string BaseUrl = "https://www.pcgamingwiki.com/w/api.php?format=json";

    public static string GetGameSearchUrl(string searchName) => GetUrl(new()
    {
        { "action", "query" },
        { "list", "search" },
        { "srlimit", "300" },
        { "srwhat", "title" },
        { "srsearch", $"\"{NormalizeSearchString(searchName)}\"" },
    });

    public static string GetGamePageUrl(string gameName) => GetUrl(new()
    {
        { "action", "parse" },
        { "page", gameName.TitleToSlug(urlEncode: false) },
    });

    public static string GetValueCountsUrl(string table, string field, string filter = null)
    {
        string having = string.IsNullOrWhiteSpace(filter) ? "Value IS NOT NULL" : $"Value LIKE '%{EscapeString(filter)}%'";

        return GetUrl(new()
        {
            { "action", "cargoquery" },
            { "limit", "max" },
            { "tables", table },
            { "fields", $"{table}.{field}=Value,COUNT(*)=Count" },
            { "group_by", $"{table}.{field}" },
            { "having", having },
        });
    }

    public static string GetGamesByHoldsUrl(string table, string field, string holds, int offset)
    {
        var args = GetBaseGameRequestParameters(table, field);
        args.Add("where", $"{table}.{field} HOLDS '{EscapeString(holds)}'");
        args.Add("offset", $"{offset:0}");

        return GetUrl(args);
    }

    public static string GetGamesByHoldsLikeUrl(string table, string field, string holds, int offset)
    {
        var args = GetBaseGameRequestParameters(table, field);
        args.Add("where", $"{table}.{field} HOLDS LIKE '{EscapeString(holds)}'");
        args.Add("offset", $"{offset:0}");

        return GetUrl(args);
    }

    public static string GetGamesByExactValuesUrl(string table, string field, IEnumerable<string> values, int offset)
    {
        var args = GetBaseGameRequestParameters(table, field);

        var valuesList = string.Join(", ", values.Select(v => $"'{EscapeString(v)}'"));

        args.Add("where", $"{table}.{field} IN ({valuesList})");
        args.Add("offset", $"{offset:0}");
        return GetUrl(args);
    }

    private static string GetUrl(Dictionary<string, string> parameters, Dictionary<string, string> continueParams = null)
    {
        StringBuilder sb = new(BaseUrl);

        AddParameters(parameters);
        AddParameters(continueParams);

        return sb.ToString();

        void AddParameters(Dictionary<string, string> localParameters)
        {
            if (localParameters == null)
                return;

            foreach (var parameter in localParameters)
            {
                sb.Append('&').Append(parameter.Key);

                if (!string.IsNullOrEmpty(parameter.Value))
                    sb.Append('=').Append(Uri.EscapeDataString(parameter.Value));
            }
        }
    }

    private JObject ExecuteRequest(string url)
    {
        try
        {
            var response = downloader.DownloadString(url);

            return JObject.Parse(response?.ResponseContent);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error performing API request");
            throw new("Error retrieving response. Check inner details for more info.", ex);
        }
    }

    private static string NormalizeSearchString(string search)
    {
        // Replace ' with " as a workaround for search API returning no results
        return search.Replace('-', ' ').Replace('\'', '"');
    }

    public List<GenericItemOption> SearchGames(string searchName)
    {
        List<GenericItemOption> gameResults = [];
        _logger.Info(searchName);

        try
        {
            JObject searchResults = ExecuteRequest(GetGameSearchUrl(searchName));

            if (searchResults.TryGetValue("error", out JToken error))
            {
                _logger.Error($"Encountered API error: {error}");
                return gameResults;
            }

            _logger.Debug($"SearchGames {searchResults["query"]["searchinfo"]["totalhits"]} results for {searchName}");

            foreach (dynamic game in searchResults["query"]["search"])
            {
                PcgwGame g = new(gameController.Settings, (string)game.title, (int)game.pageid);
                gameResults.Add(g);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error performing search");
        }

        return gameResults.OrderBy(game => NameStringCompare(searchName, game.Name)).ToList();
    }

    public void FetchGamePageContent(PcgwGame game)
    {
        game.LibraryGame = options.GameData;

        try
        {
            JObject content = ExecuteRequest(GetGamePageUrl(game.Name));

            if (content.TryGetValue("error", out JToken error))
            {
                _logger.Error($"Encountered API error: {error}");
                return;
            }

            PCGamingWikiJSONParser jsonParser = new(content, gameController);
            PcGamingWikiHtmlParser parser = new(jsonParser.PageHTMLText(), gameController);


            if (parser.CheckPageRedirect(out string redirectPage))
            {
                _logger.Debug($"redirect link: {redirectPage}");
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
            _logger.Error($"Error performing FetchGamePageContent for {game.Name}: {e}");
        }
    }

    public IEnumerable<ItemCount> GetValueCounts(string table, string field, string filter = null)
    {
        var result = Execute<CargoResultRoot<ItemCount>>(GetValueCountsUrl(table, field, filter));
        return result?.CargoQuery.Select(t => t.Title) ?? [];
    }

    public CargoResultRoot<CargoResultGame> GetGamesByHolds(string table, string field, string holds, int offset)
    {
        return Execute<CargoResultRoot<CargoResultGame>>(GetGamesByHoldsUrl(table, field, holds, offset));
    }

    public CargoResultRoot<CargoResultGame> GetGamesByHoldsLike(string table, string field, string holds, int offset)
    {
        return Execute<CargoResultRoot<CargoResultGame>>(GetGamesByHoldsLikeUrl(table, field, holds, offset));
    }

    public CargoResultRoot<CargoResultGame> GetGamesByExactValues(string table, string field, IEnumerable<string> values, int offset)
    {
        return Execute<CargoResultRoot<CargoResultGame>>(GetGamesByExactValuesUrl(table, field, values, offset));
    }

    private static Dictionary<string, string> GetBaseGameRequestParameters(string table, string field)
    {
        const string baseTable = CargoTables.Names.GameInfoBox;

        var output = new Dictionary<string, string>
        {
            { "action", "cargoquery" },
            { "limit", "max" },
            { "fields", $"{baseTable}._pageName=Name,{baseTable}.Released,{baseTable}.Available_on=OS,{baseTable}.Steam_AppID=SteamID,{baseTable}.GOGcom_ID=GOGID,{table}.{field}=Value" }
        };

        if (table == baseTable)
        {
            output.Add("tables", baseTable);
        }
        else
        {
            output.Add("tables", $"{baseTable},{table}");
            output.Add("join_on", $"{baseTable}._pageID={table}._pageID");
        }

        return output;
    }

    private T Execute<T>(string url) where T : class
    {
        try
        {
            var response = downloader.DownloadString(url);
            var data = JsonConvert.DeserializeObject<T>(response.ResponseContent);
            return data;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Error executing request");
            return null;
        }
    }

    private static string EscapeString(string str) => str?.Replace(@"\", @"\\").Replace("'", @"\'");

    // https://en.wikibooks.org/wiki/Algorithm_Implementation/Strings/Levenshtein_distance#C.23
    private static int NameStringCompare(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return Math.Max(a?.Length ?? 0, b?.Length ?? 0);

        var d = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= d.GetUpperBound(0); i++)
        {
            d[i, 0] = i;
        }

        for (int i = 0; i <= d.GetUpperBound(1); i++)
        {
            d[0, i] = i;
        }

        for (int i = 1; i <= d.GetUpperBound(0); i++)
        for (int j = 1; j <= d.GetUpperBound(1); j++)
        {
            var cost = (a[i - 1] != b[j - 1]) ? 1 : 0;

            var min1 = d[i - 1, j] + 1;
            var min2 = d[i, j - 1] + 1;
            var min3 = d[i - 1, j - 1] + cost;
            d[i, j] = Math.Min(Math.Min(min1, min2), min3);
        }

        return d[d.GetUpperBound(0), d.GetUpperBound(1)];
    }
}
