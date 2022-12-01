using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Rawg.Common
{
    public class RawgApiClient
    {

        public RawgApiClient(string key)
        {
            Key = key;
            restClient = new RestClient(new RestClientOptions { BaseUrl = new Uri("https://rawg.io/api/"), MaxTimeout = 10000 });
        }

        public string Key { get; set; }
        private ILogger logger = LogManager.GetLogger();
        private RestClient restClient;

        private T Execute<T>(RestRequest request)
        {
            return Execute<T>(request, out _);
        }

        private T Execute<T>(RestRequest request, out System.Net.HttpStatusCode statusCode)
        {
            statusCode = System.Net.HttpStatusCode.NotImplemented;

            logger.Debug($"{request.Method} {request.Resource}");
            var response = restClient.Execute(request);
            if(response == null)
            {
                logger.Debug("No response");
                return default(T);
            }
            statusCode = response.StatusCode;

            string logContent = response.Content?.Replace($"key={Key}", "key=REDACTED");
            logger.Debug($"Response code {response.StatusCode} Content: {logContent}");

            if (string.IsNullOrWhiteSpace(response.Content))
                return default(T);
            var output = JsonConvert.DeserializeObject<T>(response.Content);
            return output;
        }

        private List<T> GetAllPages<T>(RestRequest request)
        {
            var output = new List<T>();
            RawgResult<T> result;
            do
            {
                result = Execute<RawgResult<T>>(request);
                if (result?.Results != null)
                    output.AddRange(result.Results);

                request.Resource = result?.Next?
                    .TrimStart("https://api.rawg.io/api/")
                    .Replace($"&key={Key}", "")
                    .Replace($"key={Key}&", "")
                    .Replace($"key={Key}", "");
            } while (result?.Next != null);
            return output;
        }


        public RawgGameDetails GetGame(string slugOrId)
        {
            var request = new RestRequest($"games/{slugOrId}").AddKey(Key);
            return Execute<RawgGameDetails>(request);
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

            var request = new RestRequest("collections", Method.Post).AddToken(token).AddJsonBody(body);
            return Execute<RawgCollection>(request);
        }

        public bool AddGamesToCollection(string token, string collectionSlugOrId, IEnumerable<int> gameIds)
        {
            var body = new { games = gameIds.Select(i => i.ToString()).ToArray() };

            var request = new RestRequest($"collections/{collectionSlugOrId}/games", Method.Post)
                              .AddToken(token).AddJsonBody(body);
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
                            .AddJsonBody(new Dictionary<string, object>
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
                    {
                        return true;
                    }
                    else if (game is Newtonsoft.Json.Linq.JArray errorMessages)
                    {
                        string err = string.Join(", ", errorMessages);
                        logger.Warn($"Error adding {gameId} to library: {err}");
                        if (err == "This game is already in this profile")
                            return false;
                        else
                            throw new Exception(err);
                    }
                }
                throw new Exception("Error adding game to library: " + JsonConvert.SerializeObject(result));
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
                            .AddJsonBody(new Dictionary<string, object>
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
                            .AddJsonBody(new Dictionary<string, object> {
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

        private class LoginResponse
        {
            public string Key;
        }
    }

    internal static class RawgApiClientHelpers
    {
        internal static RestRequest AddToken(this RestRequest request, string token)
        {
            request.AddHeader("token", $"Token {token}");
            return request;
        }
        internal static RestRequest AddKey(this RestRequest request, string key)
        {
            request.AddQueryParameter("key", key);
            return request;
        }
    }

    public class RawgGameReviews : RawgResult<RawgReview>
    {
        public RawgReview Your { get; set; }
    }

    public class RawgReview
    {
        public long Id { get; set; }
        public int Game { get; set; }
        public int Rating { get; set; }
        public string Text { get; set; }
    }
}
