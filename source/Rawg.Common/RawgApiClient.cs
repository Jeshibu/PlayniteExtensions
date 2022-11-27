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
            Key = key == null ? key : HttpUtility.UrlEncode(key);
            restClient = new RestClient(new RestClientOptions { BaseUrl = new Uri("https://rawg.io/api/"), MaxTimeout = 10000 });
        }

        public string Key { get; set; }
        private ILogger logger = LogManager.GetLogger();
        private RestClient restClient;

        private T Get<T>(string resource)
        {
            var request = new RestRequest(resource);
            return Get<T>(request);
        }

        private T Get<T>(RestRequest request)
        {
            var response = restClient.Execute(request);
            logger.Trace(response.ResponseUri.ToString());
            logger.Trace(response.Content);
            if (string.IsNullOrWhiteSpace(response.Content))
                return default(T);
            var output = JsonConvert.DeserializeObject<T>(response.Content);
            return output;
        }

        private List<T> GetAllPages<T>(string resource)
        {
            var request = new RestRequest(resource);
            return GetAllPages<T>(request);
        }

        private List<T> GetAllPages<T>(RestRequest request)
        {
            var output = new List<T>();
            RawgResult<T> result;
            do
            {
                result = Get<RawgResult<T>>(request);
                if (result?.Results != null)
                    output.AddRange(result.Results);

                request.Resource = result?.Next?.TrimStart("https://api.rawg.io/api/");
            } while (result?.Next != null);
            return output;
        }


        public RawgGameDetails GetGame(string slugOrId)
        {
            return Get<RawgGameDetails>($"games/{slugOrId}?key={Key}");
        }

        public RawgResult<RawgGameBase> SearchGames(string query)
        {
            return Get<RawgResult<RawgGameBase>>($"games?key={Key}&search={HttpUtility.UrlEncode(query)}");
        }

        public ICollection<RawgCollection> GetCollections(string username)
        {
            return GetAllPages<RawgCollection>($"users/{username}/collections?key={Key}");
        }

        public ICollection<RawgGameDetails> GetCollectionGames(string collectionSlugOrId)
        {
            return GetAllPages<RawgGameDetails>($"collections/{collectionSlugOrId}/games?key={Key}");
        }

        public ICollection<RawgGameDetails> GetUserLibrary(string username)
        {
            return GetAllPages<RawgGameDetails>($"users/{username}/games?key={Key}");
        }

        public string Login(string username, string password)
        {
            var request = new RestRequest("auth/login", Method.Post);
            request.AlwaysMultipartFormData = true;
            request.AddParameter("email", username);
            request.AddParameter("password", password);
            var response = Get<LoginResponse>(request);
            return response?.Key;
        }

        public RawgUser GetCurrentUser(string token)
        {
            var request = new RestRequest("users/current").AddToken(token);
            return Get<RawgUser>(request);
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

        public ICollection<RawgGameDetails> GetCurrentUserLibrary(string token, string[] statuses = null)
        {
            var request = new RestRequest("users/current/games").AddToken(token);

            if(statuses != null && statuses.Any())
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
                var result = Get<Dictionary<string, object>>(request);

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
                var result = Get<Dictionary<string,object>>(request);
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
                var result = Get<Dictionary<string, object>>(request);

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
            var result = Get<Dictionary<string, object>>(request);
            return result;
        }

        public RawgReview GetCurrentUserReview(string token, int gameId)
        {
            var request = new RestRequest($"games/{gameId}/reviews", Method.Get).AddToken(token);
            var result = Get<RawgGameReviews>(request);
            return result?.Your;
        }

        public RawgReview DeleteReview(string token, long reviewId)
        {
            var request = new RestRequest($"reviews/{reviewId}", Method.Delete).AddToken(token);
            var result = Get<RawgReview>(request);
            return result;
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
    }

    public class RawgGameReviews:RawgResult<RawgReview>
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
