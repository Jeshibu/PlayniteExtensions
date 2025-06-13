using ComposableAsync;
using Newtonsoft.Json;
using Playnite.SDK;
using RateLimiter;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace MobyGamesMetadata.Api.V2;

public class MobyGamesApiClient
{
    public const string BaseUrl = "https://api.mobygames.com/v2/";
    private readonly ExecuteRestRequest restHandlerOverride;
    private readonly string apiKey;
    private readonly RestClient restClient;
    private readonly ILogger logger = LogManager.GetLogger();
    public delegate RestResponse ExecuteRestRequest(RestRequest request, CancellationToken cancellationToken = default);

    public MobyGamesApiClient(string apiKey)
    {
        this.apiKey = apiKey;
        var limiter = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromSeconds(5)).AsDelegatingHandler();
        restClient = new RestClient(new HttpClient(limiter), new RestClientOptions(BaseUrl), disposeHttpClient: true)
            .AddDefaultQueryParameter("api_key", apiKey);
    }

    public MobyGamesApiClient(ExecuteRestRequest restHandlerOverride)
    {
        this.restHandlerOverride = restHandlerOverride;
        apiKey = "something";
    }

    public IEnumerable<MobyGame> SearchGames(string searchString, CancellationToken cancellationToken = default)
    {
        var request = GetBaseGameRequest()
            .AddQueryParameter("fuzzy", "true")
            .AddQueryParameter("title", searchString);
        return ExecuteGamesRequest(request, cancellationToken);
    }

    public MobyGame GetMobyGame(int id, CancellationToken cancellationToken = default)
    {
        var request = GetBaseGameRequest().AddQueryParameter("id", id);
        var response = ExecuteGamesRequest(request, cancellationToken);
        return response?.FirstOrDefault();
    }

    public ICollection<MobyGame> GetGamesForGroup(int groupId, int limit, int offset, CancellationToken cancellationToken = default)
    {
        var request = GetBaseGameRequest(limit, offset, slim: true).AddQueryParameter("group", groupId);

        return ExecuteGamesRequest(request, cancellationToken);
    }

    public ICollection<MobyGame> GetGamesForGenre(int genreId, int limit, int offset, CancellationToken cancellationToken = default)
    {
        var request = GetBaseGameRequest(limit, offset, slim: true).AddQueryParameter("genre", genreId);

        return ExecuteGamesRequest(request, cancellationToken);
    }

    public IEnumerable<MobyGame> GetAllGamesForGroup(int groupId, GlobalProgressActionArgs progressArgs = null)
    {
        return GetAllGamesForEntity(groupId, GetGamesForGroup, "Downloading games for group...", progressArgs);
    }

    public IEnumerable<MobyGame> GetAllGamesForGenre(int genreId, GlobalProgressActionArgs progressArgs = null)
    {
        return GetAllGamesForEntity(genreId, GetGamesForGenre, "Downloading games for genre...", progressArgs);
    }

    private IEnumerable<MobyGame> GetAllGamesForEntity(int entityId, GetGames get, string progressText, GlobalProgressActionArgs progressArgs = null)
    {
        int limit = 10, offset = 0;
        List<MobyGame> response;
        do
        {
            if (progressArgs != null)
            {
                if (progressArgs.CancelToken.IsCancellationRequested)
                    yield break;

                progressArgs.Text = $"{progressText} ({offset} downloaded)";
            }

            response = get(entityId, limit, offset).ToList();
            foreach (var game in response)
                yield return game;

            offset += response.Count;
        } while (response?.Count == limit);
    }

    private T Execute<T>(RestRequest request, CancellationToken cancellationToken = default)
    {
        return Execute<T>(request, out _, cancellationToken);
    }

    private T Execute<T>(RestRequest request, out System.Net.HttpStatusCode statusCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new Exception("No Moby Games API key. Please enter one in the add-on settings.");

        statusCode = System.Net.HttpStatusCode.NotImplemented;

        logger.Debug($"{request.Method} {request.Resource}");
        if (cancellationToken.IsCancellationRequested)
        {
            logger.Debug("Request cancelled");
            return default;
        }
        var handler = restHandlerOverride ?? restClient.Execute;
        var response = handler(request, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            logger.Debug("Request cancelled");
            return default;
        }

        if (response == null)
        {
            logger.Debug("No response");
            return default;
        }
        statusCode = response.StatusCode;

        logger.Debug($"Response code {response.StatusCode}");
        logger.Trace($"Content: {response.Content}");

        if (string.IsNullOrWhiteSpace(response.Content))
            return default;

        var output = JsonConvert.DeserializeObject<T>(response.Content);
        if (output != null)
            return output;

        var error = JsonConvert.DeserializeObject<MobyGamesApiError>(response.Content);
        if (error != null)
            throw new Exception($"Error requesting {request?.Resource}: {statusCode} {error.Error}, {error.Message}");
        else
            throw new Exception($"Error requesting {request?.Resource}: {statusCode} unable to parse response: {response.Content}");
    }

    private ICollection<MobyGame> ExecuteGamesRequest(RestRequest request, CancellationToken cancellationToken)
    {
        var result = Execute<MobyGamesResult>(request, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
            return new List<MobyGame>();

        return result.games;
    }

    private RestRequest GetBaseGameRequest(int limit = 10, int offset = 0, bool slim = false)
    {
        var include = slim ? "platforms" : "covers,description,developers,game_id,genres,moby_score,moby_url,official_url,platforms,publishers,release_date,screenshots,title,highlights";

        return new RestRequest("games")
            .AddQueryParameter("include", include)
            .AddQueryParameter("limit", limit)
            .AddQueryParameter("offset", offset);
    }

    private delegate ICollection<MobyGame> GetGames(int entityId, int offset, int limit, CancellationToken cancellationToken = default);
}
