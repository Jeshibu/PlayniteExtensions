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

namespace MobyGamesMetadata.Api
{
    public class MobyGamesApiClient
    {
        public const string BaseUrl = "https://api.mobygames.com/v1/";
        private string apiKey;
        private RestClient restClient;
        private ILogger logger = LogManager.GetLogger();

        public string ApiKey
        {
            get
            {
                return apiKey;
            }
            set
            {
                if (apiKey != value && !string.IsNullOrEmpty(value))
                {
                    restClient?.Dispose();
                    var limiter = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromSeconds(1)).AsDelegatingHandler();
                    restClient = new RestClient(new HttpClient(limiter), new RestClientOptions(BaseUrl), disposeHttpClient: true)
                        .AddDefaultQueryParameter("api_key", value);
                }

                apiKey = value;
            }
        }

        private T Execute<T>(RestRequest request, CancellationToken cancellationToken = default)
        {
            return Execute<T>(request, out _, cancellationToken);
        }

        public T Execute<T>(RestRequest request, out System.Net.HttpStatusCode statusCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new Exception("No Moby Games API key. Please enter one in the add-on settings.");

            statusCode = System.Net.HttpStatusCode.NotImplemented;

            logger.Debug($"{request.Method} {request.Resource}");
            if (cancellationToken.IsCancellationRequested)
            {
                logger.Debug("Request cancelled");
                return default;
            }
            var response = restClient.Execute(request, cancellationToken);
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

        public IEnumerable<MobyGame> SearchGames(string searchString, CancellationToken cancellationToken = default)
        {
            var request = new RestRequest("games").AddQueryParameter("title", searchString);
            var result = Execute<GamesRoot>(request, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return new List<MobyGame>();

            return result.Games;
        }

        public MobyGame GetMobyGame(int id, CancellationToken cancellationToken = default)
        {
            var request = new RestRequest($"games/{id}");
            var response = Execute<MobyGame>(request, cancellationToken);
            return response;
        }

        public ICollection<MobyGame> GetGamesForGroup(int groupId, int limit, int offset, CancellationToken cancellationToken = default)
        {
            var request = new RestRequest("games")
                .AddQueryParameter("group", groupId)
                .AddQueryParameter("limit", limit)
                .AddQueryParameter("offset", offset);

            var result = Execute<GamesRoot>(request, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return new List<MobyGame>();

            return result.Games;
        }

        public ICollection<MobyGame> GetGamesForGenre(int genreId, int limit, int offset, CancellationToken cancellationToken = default)
        {
            var request = new RestRequest("games")
                .AddQueryParameter("genre", genreId)
                .AddQueryParameter("limit", limit)
                .AddQueryParameter("offset", offset);

            var result = Execute<GamesRoot>(request, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return new List<MobyGame>();

            return result.Games;
        }

        public ICollection<MobyGroup> GetGroups(int offset = 0, int limit = 100, CancellationToken cancellationToken = default)
        {
            var request = new RestRequest("groups")
                .AddQueryParameter("limit", limit)
                .AddQueryParameter("offset", offset);

            var response = Execute<GroupsRoot>(request, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return new List<MobyGroup>();

            return response.Groups;
        }

        public IEnumerable<MobyGroup> GetAllGroups(CancellationToken cancellationToken = default)
        {
            var output = new List<MobyGroup>();
            int limit = 100, offset = 0;
            //int limit = 100, offset = 9900;
            ICollection<MobyGroup> response;
            do
            {
                response = GetGroups(offset, limit, cancellationToken);
                if (response == null || cancellationToken.IsCancellationRequested) break;

                output.AddRange(response);
                offset += limit;
            } while (response.Count == limit);
            return output;
        }

        private delegate ICollection<MobyGame> GetGames(int entityId, int offset, int limit, CancellationToken cancellationToken = default);

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
            int limit = 100, offset = 0;
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

                offset += limit;
            } while (response?.Count == limit);
        }
    }
}
