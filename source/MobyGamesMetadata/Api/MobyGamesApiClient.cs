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

namespace MobyGamesMetadata.Api
{
    public class MobyGamesApiClient:ISearchableDataSourceWithDetails<GameDetails, GameDetails>
    {
        public const string BaseUrl = "https://api.mobygames.com/v1/";
        private readonly IPlatformUtility platformUtility;
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

        public MobyGamesApiClient(IPlatformUtility platformUtility)
        {
            this.platformUtility = platformUtility;
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

        public GameDetails GetGameDetails(int id)
        {
            var mobyGame = GetMobyGame(id);
            return ToGameDetails(mobyGame);
        }

        private GameDetails ToGameDetails(MobyGame mobyGame)
        {
            if (mobyGame == null) return null;
            var gameDetails = new GameDetails
            {
                Description = mobyGame.Description,
            };
            gameDetails.Names.Add(mobyGame.Title);
            if (mobyGame.AlternateTitles != null)
                gameDetails.Names.AddRange(mobyGame.AlternateTitles.Select(t => t.Title));

            if (mobyGame.SampleCover?.Image != null)
                gameDetails.CoverOptions.Add(ToIImageData(mobyGame.SampleCover));

            if (mobyGame.Genres != null)
                foreach (var genre in mobyGame.Genres)
                    AssignGenre(gameDetails, genre);

            gameDetails.Links.Add(new Link("MobyGames", mobyGame.MobyUrl));
            if (mobyGame.OfficialUrl != null)
                gameDetails.Links.Add(new Link("Official website", mobyGame.OfficialUrl));

            if (mobyGame.Platforms != null)
            {
                foreach (var platform in mobyGame.Platforms)
                {
                    gameDetails.Platforms.AddRange(platformUtility.GetPlatforms(platform.Name));
                    var releaseDate = platform.FirstReleaseDate.ParseReleaseDate(logger);
                    gameDetails.ReleaseDate = GetEarliestReleaseDate(releaseDate, gameDetails.ReleaseDate);
                }
            }

            if (mobyGame.SampleScreenshots != null)
                gameDetails.BackgroundOptions.AddRange(mobyGame.SampleScreenshots.Select(ToIImageData));

            return gameDetails;
        }

        private ReleaseDate? GetEarliestReleaseDate(ReleaseDate? r1, ReleaseDate? r2)
        {
            if (r1 == null) return r2;
            if (r2 == null) return r1;
            var dates = new List<ReleaseDate> { r1.Value, r2.Value };
            return dates.OrderBy(d => d.Date).First();
        }

        private void AssignGenre(GameDetails gameDetails, Genre g)
        {
            var list = GetRelevantList(gameDetails, g);
            if (list == null) return;
            list.Add(g.Name);
        }

        private List<string> GetRelevantList(GameDetails gameDetails, Genre g)
        {
            switch (g.Category)
            {
                case "Basic Genres":
                case "Perspective":
                case "Gameplay":
                case "Narrative Theme/Topic":
                    return gameDetails.Genres;
                case "Visual Presentation":
                case "Art Style":
                case "Pacing":
                case "Interface/Control":
                case "Sports Themes":
                case "Educational Categories":
                case "Vehicular Themes":
                case "Setting":
                case "Special Edition":
                case "Other Attributes":
                    return gameDetails.Tags;
                case "DLC/Add-on":
                default:
                    return null;
            }
        }

        private static BasicImage ToIImageData(MobyImage image)
        {
            return new BasicImage(image.Image)
            {
                Height = image.Height,
                Width = image.Width,
                ThumbnailUrl = image.ThumbnailImage,
            };
        }

        private static BasicImage ToIImageData(CoverImage image)
        {
            var img = ToIImageData((MobyImage)image);
            img.Platforms = image.Platforms;
            return img;
        }

        public IEnumerable<MobyGroup> GetAllGroups()
        {
            var output = new List<MobyGroup>();
            int limit = 100, offset = 0;
            //int limit = 100, offset = 9900;
            GroupsRoot response;
            do
            {
                var request = new RestRequest("groups")
                    .AddQueryParameter("limit", limit)
                    .AddQueryParameter("offset", offset);

                response = Execute<GroupsRoot>(request);
                if (response?.Groups == null) break;

                output.AddRange(response.Groups);
                offset += limit;
            } while (response.Groups.Count == limit);
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

        public IEnumerable<GameDetails> GetAllGameDetailsForGroup(int groupId)
        {
            return GetAllGamesForGroup(groupId).Select(ToGameDetails);
        }

        private List<MobyGame> GetGamesForGroup(int groupId, int limit, int offset)
        {
            var request = new RestRequest("games")
                .AddQueryParameter("group", groupId)
                .AddQueryParameter("limit", limit)
                .AddQueryParameter("offset", offset);

            var result = Execute<GamesRoot>(request);
            return result.Games;
        }

        GameDetails ISearchableDataSourceWithDetails<GameDetails, GameDetails>.GetDetails(GameDetails searchResult)
        {
            return searchResult;
        }

        IEnumerable<GameDetails> ISearchableDataSource<GameDetails>.Search(string query)
        {
            return SearchGames(query).Select(ToGameDetails);
        }
    }

}
