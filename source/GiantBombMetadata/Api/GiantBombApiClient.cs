using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace GiantBombMetadata.Api
{
    public interface IGiantBombApiClient
    {
        GiantBombGameDetails GetGameDetails(string gbGuid);
        GiantBombGamePropertyDetails GetGameProperty(string url);
        GiantBombSearchResultItem[] SearchGameProperties(string query);
        GiantBombSearchResultItem[] SearchGames(string query);
    }

    public class GiantBombApiClient : IGiantBombApiClient
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

    public class GiantBombGamePropertySearchProvider : ISearchableDataSourceWithDetails<GiantBombSearchResultItem, IEnumerable<GameDetails>>
    {
        private readonly IGiantBombApiClient apiClient;
        private readonly ILogger logger = LogManager.GetLogger();

        public GiantBombGamePropertySearchProvider(IGiantBombApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public IEnumerable<GameDetails> GetDetails(GiantBombSearchResultItem searchResult)
        {
            var result = apiClient.GetGameProperty($"{searchResult.ResourceType}/{searchResult.Guid}");
            return result?.Games.Select(g => new GameDetails { Names = new List<string> { g.Name }, Url = g.SiteDetailUrl }) ?? new GameDetails[0];
        }

        public IEnumerable<GiantBombSearchResultItem> Search(string query)
        {
            var result = apiClient.SearchGameProperties(query);
            return result;
        }

        public GenericItemOption<GiantBombSearchResultItem> ToGenericItemOption(GiantBombSearchResultItem item)
        {
            var output = new GenericItemOption<GiantBombSearchResultItem>(item);
            output.Name = item.Name;
            output.Description = item.Deck;
            return output;
        }
    }

    public interface IGameSearchProvider<TSearchResult> : ISearchableDataSourceWithDetails<TSearchResult, GameDetails>
    {
        /// <summary>
        /// Try to get the details from a game based on some ID found in the game (generally the links)
        /// </summary>
        /// <param name="game"></param>
        /// <param name="gameDetails">The found game details, null if nothing was found</param>
        /// <returns></returns>
        bool TryGetDetails(Game game, out GameDetails gameDetails);
    }

    public class GiantBombGameSearchProvider : IGameSearchProvider<GiantBombSearchResultItem>
    {
        private readonly IGiantBombApiClient apiClient;
        private readonly GiantBombMetadataSettings settings;
        private readonly IPlatformUtility platformUtility;
        private readonly ILogger logger = LogManager.GetLogger();

        public GiantBombGameSearchProvider(IGiantBombApiClient apiClient, GiantBombMetadataSettings settings, IPlatformUtility platformUtility)
        {
            this.apiClient = apiClient;
            this.settings = settings;
            this.platformUtility = platformUtility;
        }

        public GameDetails GetDetails(GiantBombSearchResultItem searchResult)
        {
            var result = apiClient.GetGameDetails(searchResult.Guid);
            if (result == null) return null;
            return ToGameDetails(result);
        }

        public bool TryGetDetails(Game game, out GameDetails gameDetails)
        {
            gameDetails = null;
            string guid = GiantBombHelper.GetGiantBombGuidFromGameLinks(game);
            if (guid == null)
                return false;

            var gbDetails = apiClient.GetGameDetails(guid);
            gameDetails = ToGameDetails(gbDetails);

            return gameDetails != null;
        }

        public IEnumerable<GiantBombSearchResultItem> Search(string query)
        {
            var searchOutput = new List<GiantBombSearchResultItem>();

            if (string.IsNullOrWhiteSpace(query))
                return searchOutput;

            if (Regex.IsMatch(query, @"^3030-[0-9]+$"))
            {
                try
                {
                    var gameById = apiClient.GetGameDetails(query);
                    var fakeSearchResult = new GiantBombSearchResultItem()
                    {
                        Aliases = gameById.Aliases,
                        ApiDetailUrl = gameById.ApiDetailUrl,
                        Deck = gameById.Deck,
                        Description = gameById.Description,
                        Guid = gameById.Guid,
                        Image = gameById.Image,
                        Id = gameById.Id,
                        Name = gameById.Name,
                        Platforms = gameById.Platforms,
                        ReleaseDate = gameById.ReleaseDate,
                        ResourceType = "game",
                        SiteDetailUrl = gameById.SiteDetailUrl,
                    };
                    searchOutput.Add(fakeSearchResult);
                }
                catch (Exception e)
                {
                    logger.Error(e, $"Failed to get Giant Bomb data by ID for <{query}>");
                }
            }

            try
            {
                var searchResult = apiClient.SearchGames(query);
                searchOutput.AddRange(searchResult);

            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to get Giant Bomb search data for <{query}>");
                throw;
            }


            var result = apiClient.SearchGames(query);
            return result;
        }

        public GenericItemOption<GiantBombSearchResultItem> ToGenericItemOption(GiantBombSearchResultItem item)
        {
            var output = new GenericItemOption<GiantBombSearchResultItem>(item);
            output.Name = item.Name;
            if (item.AliasesSplit.Any())
                output.Name += $" (AKA {string.Join("/", item.AliasesSplit)})";

            output.Description = item.ReleaseDate.ToString();
            if (item.Platforms?.Length > 0)
            {
                if (output.Description?.Length > 0)
                    output.Description += " | ";
                else
                    output.Description = "";

                output.Description += string.Join(", ", item.Platforms.Select(p => p.Abbreviation));
            }
            return output;
        }

        public GameDetails ToGameDetails(GiantBombGameDetails details)
        {
            if (details == null)
                return null;

            var output = new GameDetails();
            output.Names.Add(details.Name);
            output.Names.AddRange(details.AliasesSplit);
            output.Url = details.SiteDetailUrl;
            output.Description = details.Description;
            output.ReleaseDate = details.ReleaseDate.ParseReleaseDate(logger);
            output.Genres.AddRange(GetValues(PropertyImportTarget.Genres, details));
            output.Tags.AddRange(GetValues(PropertyImportTarget.Tags, details));
            if (details.Franchises != null)
            {
                output.Series.AddRange(details.Franchises.Select(f => f.Name));

                switch (settings.FranchiseSelectionMode)
                {
                    case MultiValuedPropertySelectionMode.All:
                        break;
                    case MultiValuedPropertySelectionMode.OnlyShortest:
                        output.Series = output.Series.OrderBy(f => f.Length).ThenBy(f => f).Take(1).ToList();
                        break;
                    case MultiValuedPropertySelectionMode.OnlyLongest:
                        output.Series = output.Series.OrderByDescending(f => f.Length).ThenBy(f => f).Take(1).ToList();
                        break;
                }
            }
            if (details.Developers != null)
                output.Developers.AddRange(details.Developers.Select(d => d.Name));
            if (details.Publishers != null)
                output.Publishers.AddRange(details.Publishers.Select(p => p.Name));
            if (details.Platforms != null)
                output.Platforms.AddRange(details.Platforms.SelectMany(p => platformUtility.GetPlatforms(p.Name)));
            if (details.Ratings != null)
                output.AgeRatings.AddRange(details.Ratings.Select(r => r.Name));
            if (details.Image != null)
                output.CoverOptions.Add(details.Image);
            if (details.Images != null)
                output.CoverOptions.AddRange(details.Images.Where(ImageCanBeUsedAsBackground));

            output.Url = details.SiteDetailUrl;


            return output;
        }

        private static Regex pressEventOrCoverRegex = new Regex(@"\b(e3|pax|blizzcon|box art)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static bool ImageCanBeUsedAsBackground(GiantBombImage img)
        {
            return img.Tags.Contains("screenshot", StringComparison.InvariantCultureIgnoreCase)
                || img.Tags.Contains("concept", StringComparison.InvariantCultureIgnoreCase)
                || !pressEventOrCoverRegex.IsMatch(img.Tags);
        }

        private ICollection<string> GetValues(PropertyImportTarget target, GiantBombGameDetails data)
        {
            var output = new List<string>();
            output.AddRange(GetValues(settings.Characters, target, data.Characters));
            output.AddRange(GetValues(settings.Concepts, target, data.Concepts));
            output.AddRange(GetValues(settings.Locations, target, data.Locations));
            output.AddRange(GetValues(settings.Objects, target, data.Objects));
            output.AddRange(GetValues(settings.People, target, data.People));
            output.AddRange(GetValues(settings.Themes, target, data.Themes));
            return output.OrderBy(x => x).ToList().NullIfEmpty();
        }

        private IEnumerable<string> GetValues(PropertyImportSetting importSetting, PropertyImportTarget target, GiantBombObject[] data)
        {
            if (importSetting.ImportTarget != target || data == null || data.Length == 0)
                return new string[0];

            return data.Select(d => $"{importSetting.Prefix}{d.Name}");
        }
    }

    public interface IGameSearchResult
    {
        string Name { get; }
        IEnumerable<string> AlternateNames { get; }
        IEnumerable<string> Platforms { get; }
        ReleaseDate? ReleaseDate { get; }
    }

    public abstract class GenericMetadataProvider<TSearchResult> : OnDemandMetadataProvider where TSearchResult : IGameSearchResult
    {
        private readonly IGameSearchProvider<TSearchResult> dataSource;
        private readonly MetadataRequestOptions options;
        private readonly IPlayniteAPI playniteApi;
        private readonly IPlatformUtility platformUtility;
        private ILogger logger = LogManager.GetLogger();
        private GameDetails foundGame = null;

        protected GenericMetadataProvider(IGameSearchProvider<TSearchResult> dataSource, MetadataRequestOptions options, IPlayniteAPI playniteApi, IPlatformUtility platformUtility)
        {
            this.dataSource = dataSource;
            this.options = options;
            this.playniteApi = playniteApi;
            this.platformUtility = platformUtility;
        }

        protected virtual GameDetails GetGameDetails()
        {
            if (foundGame != null)
                return foundGame;

            if (foundGame == null)
            {
                if (options.IsBackgroundDownload && dataSource.TryGetDetails(options.GameData, out var details))
                    return foundGame = details;

                var searchResult = GetSearchResultGame();
                if (searchResult != null)
                    return foundGame = dataSource.GetDetails(searchResult);
            }
            return foundGame = new GameDetails();
        }

        protected virtual TSearchResult GetSearchResultGame()
        {
            if (options.IsBackgroundDownload)
            {
                if (string.IsNullOrWhiteSpace(options.GameData.Name))
                    return default;

                var searchResult = dataSource.Search(options.GameData.Name);

                if (searchResult == null)
                    return default;

                var snc = new SortableNameConverter(new string[0], batchOperation: false, numberLength: 1, removeEditions: true);

                var nameToMatch = snc.Convert(options.GameData.Name).Deflate();

                var matchedGames = searchResult.Where(g => HasMatchingName(g, nameToMatch, snc) && HasPlatformOverlap(g)).ToList();

                switch (matchedGames.Count)
                {
                    case 0:
                        return default;
                    case 1:
                        return matchedGames.First();
                    default:
                        var searchReleaseDate = options.GameData.ReleaseDate;
                        var sortedByReleaseDateProximity = matchedGames.OrderBy(g => GetDaysApart(searchReleaseDate, g.ReleaseDate)).ToList();
                        return sortedByReleaseDateProximity.First();
                }
            }
            else
            {
                var selectedGame = playniteApi.Dialogs.ChooseItemWithSearch(null, (a) =>
                {
                    var searchOutput = new List<GenericItemOption>();

                    if (string.IsNullOrWhiteSpace(a))
                        return searchOutput;

                    try
                    {
                        var searchResult = dataSource.Search(a);
                        searchOutput.AddRange(searchResult.Select(dataSource.ToGenericItemOption));

                    }
                    catch (Exception e)
                    {
                        logger.Error(e, $"Failed to get Giant Bomb search data for <{a}>");
                    }

                    return searchOutput;
                }, options.GameData.Name, string.Empty);

                var selectedGameReal = (GenericItemOption<TSearchResult>)selectedGame;
                return selectedGameReal == null ? default : selectedGameReal.Item;
            }
        }

        private int GetDaysApart(ReleaseDate? searchDate, ReleaseDate? resultDate)
        {
            if (searchDate == null)
                return 0;

            if (resultDate == null)
                return 365 * 2; //allow anything within a year to take precedence over this

            var daysApart = (searchDate.Value.Date.Date - resultDate.Value.Date.Date).TotalDays;

            return Math.Abs((int)daysApart);
        }

        private static bool HasMatchingName(IGameSearchResult g, string deflatedSearchName, SortableNameConverter snc)
        {
            var gameNames = new List<string> { g.Name };
            if (g.AlternateNames?.Any() == true)
                gameNames.AddRange(g.AlternateNames);

            foreach (var gameName in gameNames)
            {
                var deflatedGameName = snc.Convert(gameName).Deflate();
                if (deflatedSearchName.Equals(deflatedGameName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasPlatformOverlap(IGameSearchResult g)
        {
            var searchGamePlatforms = options.GameData.Platforms;
            var resultGamePlatformNames = g.Platforms?.ToList();
            if (searchGamePlatforms == null || searchGamePlatforms.Count == 0 || resultGamePlatformNames == null || resultGamePlatformNames.Count == 0)
                return true; //no search platforms acts as a wildcard - everything could be a match

            var gbPlatforms = resultGamePlatformNames.SelectMany(platformUtility.GetPlatforms).ToList();
            foreach (var gbp in gbPlatforms)
            {
                if (gbp is MetadataSpecProperty specProperty)
                {
                    if (searchGamePlatforms.Any(p => specProperty.Id == p.SpecificationId))
                        return true;
                }
                else if (gbp is MetadataNameProperty nameProperty)
                {
                    if (searchGamePlatforms.Any(p => nameProperty.Name == p.Name))
                        return true;
                }
            }
            return false;
        }

        public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
        {
            return GetGameDetails().AgeRatings.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            return SelectImage(GetGameDetails().BackgroundOptions);
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            return GetGameDetails().CommunityScore;
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            return SelectImage(GetGameDetails().CoverOptions);
        }

        public override int? GetCriticScore(GetMetadataFieldArgs args)
        {
            return GetGameDetails().CriticScore;
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            return GetGameDetails().Description;
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            return GetGameDetails().Developers.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
        {
            return GetGameDetails().Features.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            return GetGameDetails().Genres.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override MetadataFile GetIcon(GetMetadataFieldArgs args)
        {
            return SelectImage(GetGameDetails().IconOptions);
        }

        public override ulong? GetInstallSize(GetMetadataFieldArgs args)
        {
            return GetGameDetails().InstallSize;
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            return GetGameDetails().Links?.NullIfEmpty();
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            return GetGameDetails().Names.First();
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            return GetGameDetails().Platforms.NullIfEmpty();
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            return GetGameDetails().Publishers.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override IEnumerable<MetadataProperty> GetRegions(GetMetadataFieldArgs args)
        {
            return base.GetRegions(args);
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            return GetGameDetails().ReleaseDate;
        }

        public override IEnumerable<MetadataProperty> GetSeries(GetMetadataFieldArgs args)
        {
            return GetGameDetails().Series.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            return GetGameDetails().Tags.NullIfEmpty()?.Select(ToNameProperty);
        }

        protected MetadataFile SelectImage(List<IImageData> images)
        {
            if (images == null || images.Count == 0)
                return null;

            if (options.IsBackgroundDownload)
            {
                return new MetadataFile(images.First().Url);
            }
            else
            {
                var imageOptions = images?.Select(i => new ImgOption(i)).ToList<ImageFileOption>();
                var selected = playniteApi.Dialogs.ChooseImageFile(imageOptions, "Select background");
                var fullSizeUrl = (selected as ImgOption)?.Image.Url;

                if (fullSizeUrl == null)
                    return null;

                return new MetadataFile(fullSizeUrl);
            }
        }

        protected class ImgOption : ImageFileOption
        {
            public ImgOption(IImageData image)
            {
                Image = image;
                Path = image.ThumbnailUrl ?? image.Url;
            }

            public IImageData Image { get; }
        }

        protected static MetadataProperty ToNameProperty(string name)
        {
            return new MetadataNameProperty(name);
        }
    }
}