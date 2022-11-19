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

        public RawgApiClient(IWebDownloader downloader, string key)
        {
            Downloader = downloader;
            Key = HttpUtility.UrlEncode(key);
            restClient = new RestClient(new RestClientOptions { BaseUrl = new Uri("https://rawg.io/api/"), MaxTimeout = 10000 });
        }

        public IWebDownloader Downloader { get; }
        public string Key { get; set; }
        private ILogger logger = LogManager.GetLogger();
        private RestClient restClient;

        private T Get<T>(string url)
        {
            var response = Downloader.DownloadString(url, throwExceptionOnErrorResponse: true);
            var output = JsonConvert.DeserializeObject<T>(response.ResponseContent);
            return output;
        }

        private T Get<T>(RestRequest request)
        {
            var response = restClient.Execute(request);
            logger.Trace(response.ResponseUri.ToString());
            logger.Trace(response.Content);
            var output = JsonConvert.DeserializeObject<T>(response.Content);
            return output;
        }

        private T GetAuthenticated<T>(string token, RestRequest request)
        {
            request.AddHeader("token", $"Token {token}");
            return Get<T>(request);
        }

        public RawgGameDetails GetGame(string slugOrId)
        {
            return Get<RawgGameDetails>($"https://api.rawg.io/api/games/{slugOrId}?key={Key}");
        }

        public RawgResult<RawgGameBase> SearchGames(string query)
        {
            return Get<RawgResult<RawgGameBase>>($"https://rawg.io/api/games?key={Key}&search={HttpUtility.UrlEncode(query)}");
        }

        public RawgResult<RawgCollection> GetCollections(string username)
        {
            return Get<RawgResult<RawgCollection>>($"https://rawg.io/api/users/{username}/collections?key={Key}");
        }

        public RawgResult<RawgGameDetails> GetCollectionGames(string collectionSlugOrId)
        {
            return Get<RawgResult<RawgGameDetails>>($"https://rawg.io/api/collections/{collectionSlugOrId}/games?key={Key}");
        }

        public RawgResult<RawgGameDetails> GetUserLibrary(string username)
        {
            return Get<RawgResult<RawgGameDetails>>($"https://rawg.io/api/users/{username}/games?key={Key}");
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
            var request = new RestRequest("users/current");
            return GetAuthenticated<RawgUser>(token, request);
        }

        public RawgResult<RawgCollection> GetCurrentUserCollections(string token)
        {
            var request = new RestRequest("users/current/collections");
            return GetAuthenticated<RawgResult<RawgCollection>>(token, request);
        }

        public RawgResult<RawgGameDetails> GetCurrentUserCollectionGames(string collectionSlugOrId, string token)
        {
            var request = new RestRequest($"collections/{collectionSlugOrId}/games");
            return GetAuthenticated<RawgResult<RawgGameDetails>>(token, request);
        }

        public RawgResult<RawgGameDetails> GetCurrentUserLibrary(string token)
        {
            var request = new RestRequest("users/current/games");
            return GetAuthenticated<RawgResult<RawgGameDetails>>(token, request);
        }

        public bool AddGameToLibrary(string token, int gameId, string completionStatus)
        {
            var request = new RestRequest("users/current/games", Method.Post)
                            .AddJsonBody(new Dictionary<string, object>
                            {
                                { "game", gameId },
                                { "status", completionStatus },
                            });
            try
            {
                var result = GetAuthenticated<Dictionary<string, object>>(token, request);

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

        public bool UpdateGameCompletionStatus(string token, int gameId, string completionStatus)
        {
            var request = new RestRequest($"users/current/games/{gameId}", Method.Patch)
                            .AddJsonBody(new Dictionary<string, object>
                            {
                                { "status", completionStatus },
                            });
            try
            {
                var result = GetAuthenticated<Dictionary<string, object>>(token, request);

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
                            .AddJsonBody(new Dictionary<string, object> {
                                { "game", gameId },
                                { "rating", rating },
                                { "add_to_library", addToLibrary },
                            });
            var result = GetAuthenticated<Dictionary<string, object>>(token, request);
            return result;
        }

        private class LoginResponse
        {
            public string Key;
        }
    }
}
