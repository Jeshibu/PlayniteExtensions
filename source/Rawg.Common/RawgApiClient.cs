using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteExtensions.Common;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Rawg.Common;

public partial class RawgApiClient(string key)
{
    private string Key { get; } = key;
    private readonly ILogger logger = LogManager.GetLogger();
    private readonly RestClient restClient = new(new RestClientOptions { BaseUrl = new("https://rawg.io/api/"), MaxTimeout = 10000 });

    private T Execute<T>(RestRequest request)
    {
        return Execute<T>(request, out _);
    }

    private T Execute<T>(RestRequest request, out System.Net.HttpStatusCode statusCode)
    {
        logger.Debug($"{request.Method} {request.Resource}");
        var response = restClient.Execute(request);
        statusCode = response.StatusCode;

        string logContent = response.Content?.Replace($"key={Key}", "key=REDACTED");
        logger.Debug($"Response code {response.StatusCode} Content: {logContent}");

        if (string.IsNullOrWhiteSpace(response.Content))
            return default;

        var output = JsonConvert.DeserializeObject<T>(response.Content);
        return output;
    }

    private List<T> GetAllPages<T>(RestRequest request, GlobalProgressActionArgs a = null, string baseProgressString = "Downloading RAWG data…")
    {
        a?.IsIndeterminate = false;
        a?.Text = baseProgressString;
        var output = new List<T>();
        RawgResult<T> result;
        do
        {
            result = Execute<RawgResult<T>>(request);
            if (result == null)
                continue;

            if (result.Results != null)
                output.AddRange(result.Results);

            if (a != null && (int)a.ProgressMaxValue != result.Count)
                a.ProgressMaxValue = result.Count;

            a?.CurrentProgressValue = output.Count;
            a?.Text = $"""
                       {baseProgressString}
                       {output.Count}/{result.Count}
                       """;

            if (result.Next == null)
                continue;

            request.Resource = result.Next
                .TrimStart("https://api.rawg.io/api/")
                .Replace($"&key={Key}", "")
                .Replace($"key={Key}&", "")
                .Replace($"key={Key}", "");
        }
        while (result?.Next != null && a?.CancelToken.IsCancellationRequested != true);

        return output;
    }


    public RawgGameDetails GetGame(string slugOrId)
    {
        var request = new RestRequest($"games/{slugOrId}").AddKey(Key);
        return Execute<RawgGameDetails>(request);
    }

    public ICollection<RawgScreenshot> GetScreenshots(string gameSlugOrId)
    {
        var request = new RestRequest($"games/{gameSlugOrId}/screenshots").AddKey(Key);
        return GetAllPages<RawgScreenshot>(request);
    }

    public RawgResult<RawgGameBase> SearchGames(string query)
    {
        var request = new RestRequest($"games?search={HttpUtility.UrlEncode(query)}").AddKey(Key);
        return Execute<RawgResult<RawgGameBase>>(request);
    }

    public ICollection<RawgCollection> GetCollections(string username)
    {
        var request = new RestRequest($"users/{username}/collections").AddKey(Key);
        return GetAllPages<RawgCollection>(request);
    }

    public ICollection<RawgGameDetails> GetCollectionGames(string collectionSlugOrId)
    {
        var request = new RestRequest($"collections/{collectionSlugOrId}/games").AddKey(Key);
        return GetAllPages<RawgGameDetails>(request);
    }

    public ICollection<RawgGameDetails> GetUserLibrary(string username)
    {
        var request = new RestRequest($"users/{username}/games").AddKey(Key);
        return GetAllPages<RawgGameDetails>(request);
    }

    public string Login(string username, string password)
    {
        var request = new RestRequest("auth/login", Method.Post);
        request.AlwaysMultipartFormData = true;
        request.AddParameter("email", username);
        request.AddParameter("password", password);
        var response = Execute<LoginResponse>(request);
        return response?.Key;
    }

    public RawgUser GetCurrentUser(string token)
    {
        var request = new RestRequest("users/current").AddToken(token);
        return Execute<RawgUser>(request);
    }

    public ICollection<RawgCollection> GetCurrentUserCollections(string token)
    {
        var request = new RestRequest("users/current/collections").AddToken(token);
        return GetAllPages<RawgCollection>(request);
    }

    public ICollection<RawgGameDetails> GetCurrentUserCollectionGames(string collectionSlugOrId, string token)
    {
        var request = new RestRequest($"collections/{collectionSlugOrId}/games").AddToken(token);
        return GetAllPages<RawgGameDetails>(request);
    }

    public RawgCollection CreateCollection(string token, string name, string description, bool isPrivate)
    {
        var body = new Dictionary<string, object> {
            { "name", name },
            { "description", description },
            { "is_private", isPrivate },
        };

        var request = new RestRequest("collections", Method.Post).AddToken(token).AddJsonBody2(body);
        return Execute<RawgCollection>(request);
    }

    public bool AddGamesToCollection(string token, string collectionSlugOrId, IEnumerable<int> gameIds)
    {
        var body = new { games = gameIds.Select(i => i.ToString()).ToArray() };

        var request = new RestRequest($"collections/{collectionSlugOrId}/games", Method.Post)
                          .AddToken(token).AddJsonBody2(body);
        var result = Execute<Dictionary<string, object>>(request);
        return result.ContainsKey("games");
    }

    public ICollection<RawgGameDetails> GetCurrentUserLibrary(string token, string[] statuses = null)
    {
        var request = new RestRequest("users/current/games").AddToken(token);

        if (statuses != null && statuses.Any())
        {
            request.AddQueryParameter("statuses", string.Join(",", statuses));
        }

        return GetAllPages<RawgGameDetails>(request);
    }

    public ICollection<RawgGameDetails> GetCurrentUserWishlist(string token)
    {
        var request = new RestRequest("users/current/games")
                     .AddToken(token)
                     .AddQueryParameter("statuses", "toplay");
        return GetAllPages<RawgGameDetails>(request);
    }

    public bool AddGameToLibrary(string token, int gameId, string completionStatus)
    {
        var request = new RestRequest("users/current/games", Method.Post)
                        .AddToken(token)
                        .AddJsonBody2(new Dictionary<string, object>
                        {
                            { "game", gameId },
                            { "status", completionStatus },
                        });
        try
        {
            var result = Execute<Dictionary<string, object>>(request);

            if (result.TryGetValue("game", out object game))
            {
                if (game is int resultGameId && resultGameId == gameId)
                    return true;

                if (game is Newtonsoft.Json.Linq.JArray errorMessages)
                {
                    string err = string.Join(", ", errorMessages);
                    logger.Warn($"Error adding {gameId} to library: {err}");
                    return err switch
                    {
                        "This game is already in this profile" => false,
                        _ => throw new(err)
                    };
                }
            }
            throw new("Error adding game to library: " + JsonConvert.SerializeObject(result));
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error adding game {gameId} to library");
            return false;
        }
    }

    public bool DeleteGameFromLibrary(string token, int gameId)
    {
        var request = new RestRequest($"users/current/games/{gameId}", Method.Delete).AddToken(token);
        try
        {
            var result = Execute<Dictionary<string, object>>(request);
            if (result != null && result.ContainsKey("detail"))
            {
                logger.Info($"Could not delete RAWG game {gameId} from user library: {result["detail"]}");
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error deleting game {gameId}");
            return false;
        }
    }

    public bool UpdateGameCompletionStatus(string token, int gameId, string completionStatus)
    {
        var request = new RestRequest($"users/current/games/{gameId}", Method.Patch)
                        .AddToken(token)
                        .AddJsonBody2(new Dictionary<string, object>
                        {
                            { "status", completionStatus },
                        });
        try
        {
            var result = Execute<Dictionary<string, object>>(request);

            if (result.TryGetValue("game", out object game))
            {
                if (game is long resultGameId && resultGameId == gameId)
                    return true;
            }
            logger.Warn($"Error updating {gameId} status: " + JsonConvert.SerializeObject(result));
            return false;
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error updating game {gameId} completion status");
            return false;
        }
    }

    public Dictionary<string, object> RateGame(string token, int gameId, int rating, bool addToLibrary = false)
    {
        var request = new RestRequest("reviews", Method.Post)
                        .AddToken(token)
                        .AddJsonBody2(new Dictionary<string, object> {
                            { "game", gameId },
                            { "rating", rating },
                            { "add_to_library", addToLibrary },
                        });
        var result = Execute<Dictionary<string, object>>(request);
        return result;
    }

    public RawgReview GetCurrentUserReview(string token, int gameId)
    {
        var request = new RestRequest($"games/{gameId}/reviews", Method.Get).AddToken(token).AddKey(Key);
        var result = Execute<RawgGameReviews>(request);
        return result?.Your;
    }

    public bool DeleteReview(string token, long reviewId)
    {
        var request = new RestRequest($"reviews/{reviewId}", Method.Delete).AddToken(token);
        var result = Execute<Dictionary<string, object>>(request, out var statusCode);

        return statusCode == System.Net.HttpStatusCode.Accepted;
    }
}

internal static class RawgApiClientHelpers
{
    extension(RestRequest request)
    {
        internal RestRequest AddToken(string token) => request.AddHeader("token", $"Token {token}");

        internal RestRequest AddKey(string key) => request.AddQueryParameter("key", key);

        internal RestRequest AddJsonBody2(object obj)
        {
            var body = JsonConvert.SerializeObject(obj);
            return request.AddBody(body);
        }
    }
}
