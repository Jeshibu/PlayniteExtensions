using Newtonsoft.Json;
using Playnite.SDK;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PCGamingWikiBulkImport.DataCollection;

public interface ICargoQuery
{
    CargoResultRoot<CargoResultGame> GetGamesByExactValues(string table, string field, IEnumerable<string> values, int offset);
    CargoResultRoot<CargoResultGame> GetGamesByHolds(string table, string field, string holds, int offset);
    CargoResultRoot<CargoResultGame> GetGamesByHoldsLike(string table, string field, string holds, int offset);
    IEnumerable<ItemCount> GetValueCounts(string table, string field, string filter = null);
}

internal class CargoQuery : ICargoQuery
{
    private readonly ILogger logger = LogManager.GetLogger();

    private readonly RestClient restClient = new RestClient("https://www.pcgamingwiki.com/w/api.php")
        .AddDefaultQueryParameter("action", "cargoquery")
        .AddDefaultQueryParameter("limit", "max")
        .AddDefaultQueryParameter("format", "json");

    public IEnumerable<ItemCount> GetValueCounts(string table, string field, string filter = null)
    {
        string having = "Value IS NOT NULL";

        if (!string.IsNullOrWhiteSpace(filter))
            having = $"Value LIKE '%{EscapeString(filter)}%'";

        var request = new RestRequest()
                .AddQueryParameter("tables", table)
                .AddQueryParameter("fields", $"{table}.{field}=Value,COUNT(*)=Count")
                .AddQueryParameter("group_by", $"{table}.{field}")
                .AddQueryParameter("having", having);

        var result = Execute<CargoResultRoot<ItemCount>>(request);
        return result?.CargoQuery.Select(t => t.Title) ?? [];
    }

    public CargoResultRoot<CargoResultGame> GetGamesByHolds(string table, string field, string holds, int offset)
    {
        var request = GetBaseGameRequest(table, field)
            .AddQueryParameter("where", $"{table}.{field} HOLDS '{EscapeString(holds)}'")
            .AddQueryParameter("offset", $"{offset:0}");

        return Execute<CargoResultRoot<CargoResultGame>>(request);
    }

    public CargoResultRoot<CargoResultGame> GetGamesByHoldsLike(string table, string field, string holds, int offset)
    {
        var request = GetBaseGameRequest(table, field)
            .AddQueryParameter("where", $"{table}.{field} HOLDS LIKE '{EscapeString(holds)}'")
            .AddQueryParameter("offset", $"{offset:0}");

        return Execute<CargoResultRoot<CargoResultGame>>(request);
    }

    public CargoResultRoot<CargoResultGame> GetGamesByExactValues(string table, string field, IEnumerable<string> values, int offset)
    {
        var valuesList = string.Join(", ", values.Select(v => $"'{EscapeString(v)}'"));

        var request = GetBaseGameRequest(table, field)
            .AddQueryParameter("where", $"{table}.{field} IN ({valuesList})")
            .AddQueryParameter("offset", $"{offset:0}");

        return Execute<CargoResultRoot<CargoResultGame>>(request);
    }

    private static RestRequest GetBaseGameRequest(string table, string field)
    {
        const string baseTable = CargoTables.Names.GameInfoBox;

        var request = new RestRequest()
                .AddQueryParameter("fields", $"{baseTable}._pageName=Name,{baseTable}.Released,{baseTable}.Available_on=OS,{baseTable}.Steam_AppID=SteamID,{baseTable}.GOGcom_ID=GOGID,{table}.{field}=Value");

        if (table == baseTable)
            request.AddQueryParameter("tables", baseTable);
        else
            request.AddQueryParameter("tables", $"{baseTable},{table}")
                   .AddQueryParameter("join_on", $"{baseTable}._pageID={table}._pageID");

        return request;
    }

    private T Execute<T>(RestRequest request) where T : class
    {
        var response = restClient.Execute(request);
        try
        {
            var data = JsonConvert.DeserializeObject<T>(response.Content);
            return data;
        }
        catch (Exception ex)
        {
            logger.Warn(ex, "Error executing request");
            return null;
        }
    }

    private static string EscapeString(string str) => str?.Replace(@"\", @"\\").Replace("'", @"\'");
}
