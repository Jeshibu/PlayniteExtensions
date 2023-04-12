using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playnite.SDK;
using System.Text.Json.Serialization;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;
using Playnite.SDK.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net.Http;
using RateLimiter;
using ComposableAsync;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;

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

        public IEnumerable<MobyGame> SearchGames(string searchString)
        {
            var request = new RestRequest("games").AddQueryParameter("title", searchString);
            var result = Execute<GamesRoot>(request);
            return result.Games;
        }

        public MobyGame GetMobyGame(int id)
        {
            var request = new RestRequest($"games/{id}");
            var response = Execute<MobyGame>(request);
            return response;
        }

        public ICollection<MobyGame> GetGamesForGroup(int groupId, int limit, int offset)
        {
            var request = new RestRequest("games")
                .AddQueryParameter("group", groupId)
                .AddQueryParameter("limit", limit)
                .AddQueryParameter("offset", offset);

            var result = Execute<GamesRoot>(request);
            return result.Games;
        }

        public ICollection<MobyGroup> GetGroups(int offset = 0, int limit = 100)
        {
            var request = new RestRequest("groups")
                .AddQueryParameter("limit", limit)
                .AddQueryParameter("offset", offset);

            var response = Execute<GroupsRoot>(request);
            return response.Groups;
        }

        public IEnumerable<MobyGroup> GetAllGroups()
        {
            var output = new List<MobyGroup>();
            int limit = 100, offset = 0;
            //int limit = 100, offset = 9900;
            ICollection<MobyGroup> response;
            do
            {
                 response = GetGroups(offset, limit);
                if (response == null) break;

                output.AddRange(response);
                offset += limit;
            } while (response.Count == limit);
            return output;
        }

        public IEnumerable<MobyGame> GetAllGamesForGroup(int groupId)
        {
            int limit = 100, offset = 0;
            List<MobyGame> response;
            do
            {
                response = GetGamesForGroup(groupId, limit, offset).ToList();
                foreach (var game in response)
                    yield return game;

                offset += limit;
            } while (response?.Count == limit);
        }
    }
}
