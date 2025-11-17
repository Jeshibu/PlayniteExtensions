using Newtonsoft.Json;
using Playnite.SDK;
using RestSharp;
using System;
using System.Threading;

namespace GiantBombMetadata.Api;

public interface IGiantBombApiClient
{
    GiantBombGameDetails GetGameDetails(string gbGuid, CancellationToken cancellationToken);
    GiantBombGamePropertyDetails GetGameProperty(string url, CancellationToken cancellationToken);
    GiantBombSearchResultItem[] SearchGameProperties(string query, CancellationToken cancellationToken);
    GiantBombSearchResultItem[] SearchGames(string query, CancellationToken cancellationToken);
    GiantBombSearchResultItem[] GetGenres(CancellationToken cancellationToken);
    GiantBombSearchResultItem[] GetThemes(CancellationToken cancellationToken);
}

public class GiantBombApiClient : IGiantBombApiClient, IDisposable
{
    public void Dispose()
    {
        if (!disposed)
            restClient?.Dispose();

        disposed = true;
    }

    ~GiantBombApiClient()
    {
        Dispose();
    }

    private const string BaseUrl = "https://www.giantbomb.com/api/";
    private RestClient restClient;
    private readonly ILogger logger = LogManager.GetLogger();
    private bool disposed = false;

    public string ApiKey
    {
        get;
        set
        {
            if (field != value && !string.IsNullOrEmpty(value))
            {
                restClient?.Dispose();
                restClient = new RestClient(BaseUrl)
                             .AddDefaultQueryParameter("api_key", value)
                             .AddDefaultQueryParameter("format", "json");
            }

            field = value;
        }
    }

    private T Execute<T>(RestRequest request, CancellationToken cancellationToken = default)
    {
        return Execute<T>(request, out _, cancellationToken);
    }

    private T Execute<T>(RestRequest request, out System.Net.HttpStatusCode statusCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new Exception("No Giant Bomb API key. Please enter one in the add-on settings.");

        statusCode = System.Net.HttpStatusCode.NotImplemented;

        logger.Debug($"{request.Method} {request.Resource}");
        var response = restClient.Execute(request, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            logger.Debug("Request cancelled");
            return default;
        }

        statusCode = response.StatusCode;

        logger.Debug($"Response code {response.StatusCode}");
        logger.Trace($"Content: {response.Content}");

        if (string.IsNullOrWhiteSpace(response.Content))
            return default;
        var output = JsonConvert.DeserializeObject<GiantBombResponse<T>>(response.Content);
        if (output?.Error != "OK")
            throw new Exception($"Error requesting {request?.Resource}: {output?.Error}");

        return output.Results;
    }

    public GiantBombGameDetails GetGameDetails(string gbGuid, CancellationToken cancellationToken)
    {
        var request = new RestRequest($"game/{gbGuid}");
        return Execute<GiantBombGameDetails>(request, cancellationToken);
    }

    public GiantBombGamePropertyDetails GetGameProperty(string url, CancellationToken cancellationToken)
    {
        if (url.StartsWith(BaseUrl))
            url = url.Remove(0, BaseUrl.Length);

        var request = new RestRequest(url)
            .AddQueryParameter("field_list", "aliases,api_detail_url,deck,games,guid,id,name,site_detail_url");
        return Execute<GiantBombGamePropertyDetails>(request, cancellationToken);
    }

    public GiantBombSearchResultItem[] Search(string query, string resources, CancellationToken cancellationToken)
    {
        var request = new RestRequest("search")
            .AddQueryParameter("query", query)
            .AddQueryParameter("resources", resources);

        return Execute<GiantBombSearchResultItem[]>(request, cancellationToken);
    }

    public GiantBombSearchResultItem[] SearchGames(string query, CancellationToken cancellationToken) => Search(query, "game", cancellationToken);
    public GiantBombSearchResultItem[] SearchGameProperties(string query, CancellationToken cancellationToken) => Search(query, "character,concept,object,location,person,franchise", cancellationToken);

    public GiantBombSearchResultItem[] GetGenres(CancellationToken cancellationToken)
    {
        var request = new RestRequest("genres")
            .AddQueryParameter("field_list", "api_detail_url,deck,guid,id,name,site_detail_url");

        return Execute<GiantBombSearchResultItem[]>(request, cancellationToken);
    }

    public GiantBombSearchResultItem[] GetThemes(CancellationToken cancellationToken)
    {
        var request = new RestRequest("themes");
        return Execute<GiantBombSearchResultItem[]>(request, cancellationToken);
    }
}