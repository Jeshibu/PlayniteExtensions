using Newtonsoft.Json;
using Playnite.SDK;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GiantBombMetadata.Api
{
    public class GiantBombApiClient
    {
        public const string BaseUrl = "https://www.giantbomb.com/api/";
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
                    restClient = new RestClient(BaseUrl)
                        .AddDefaultQueryParameter("api_key", value)
                        .AddDefaultQueryParameter("format", "json");
                }

                apiKey = value;
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
                return default(T);
            }

            if (response == null)
            {
                logger.Debug("No response");
                return default(T);
            }
            statusCode = response.StatusCode;

            logger.Debug($"Response code {response.StatusCode}");
            logger.Trace($"Content: {response.Content}");

            if (string.IsNullOrWhiteSpace(response.Content))
                return default(T);
            var output = JsonConvert.DeserializeObject<GiantBombResponse<T>>(response.Content);
            if (output?.Error != "OK")
            {
                throw new Exception($"Error requesting {request?.Resource}: {output?.Error}");
            }
            return output.Results;
        }

        public GiantBombGameDetails GetGameDetails(string gbGuid)
        {
            var request = new RestRequest($"game/{gbGuid}");
            return Execute<GiantBombGameDetails>(request);
        }

        public GiantBombGamePropertyDetails GetGameProperty(string url)
        {
            if (url.StartsWith(BaseUrl))
                url = url.Remove(0, BaseUrl.Length);

            var request = new RestRequest(url);
            return Execute<GiantBombGamePropertyDetails>(request);
        }

        public GiantBombSearchResultItem[] Search(string query, string resources)
        {
            var request = new RestRequest("search")
                .AddQueryParameter("query", query)
                .AddQueryParameter("resources", resources);

            return Execute<GiantBombSearchResultItem[]>(request);
        }

        public GiantBombSearchResultItem[] SearchGames(string query) => Search(query, "game");
        public GiantBombSearchResultItem[] SearchGameProperties(string query) => Search(query, "character,concept,object,person");

        //TODO: figure out how to get games for locations (and maybe for themes too)
        //public GiantBombSearchResultItem[] SearchGameProperties(string query) => Search(query, "character,concept,object,location,person");
    }
}
