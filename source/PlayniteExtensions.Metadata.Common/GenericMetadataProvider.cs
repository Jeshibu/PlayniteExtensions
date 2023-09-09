using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using PlayniteExtensions.Common;

namespace PlayniteExtensions.Metadata.Common
{
    public abstract class GenericMetadataProvider<TSearchResult> : OnDemandMetadataProvider where TSearchResult : IGameSearchResult
    {
        private readonly IGameSearchProvider<TSearchResult> dataSource;
        private readonly MetadataRequestOptions options;
        private readonly List<Platform> requestPlatforms;
        private readonly IPlayniteAPI playniteApi;
        private readonly IPlatformUtility platformUtility;
        private ILogger logger = LogManager.GetLogger();
        private GameDetails foundGame = null;
        protected abstract string ProviderName { get; }

        protected GenericMetadataProvider(IGameSearchProvider<TSearchResult> dataSource, MetadataRequestOptions options, IPlayniteAPI playniteApi, IPlatformUtility platformUtility)
        {
            this.dataSource = dataSource;
            this.options = options;
            this.playniteApi = playniteApi;
            this.platformUtility = platformUtility;
            requestPlatforms = options.GameData.Platforms;
        }

        protected virtual GameDetails GetGameDetails(GetMetadataFieldArgs args)
        {
            if (foundGame != null)
                return foundGame;

            if (foundGame == null)
            {
                if (options.IsBackgroundDownload && dataSource.TryGetDetails(options.GameData, out var details, args.CancelToken))
                    return foundGame = details;

                var searchResult = GetSearchResultGame(args);
                if (searchResult != null)
                    return foundGame = dataSource.GetDetails(searchResult);
            }
            return foundGame = new GameDetails();
        }

        protected virtual TSearchResult GetSearchResultGame(GetMetadataFieldArgs args)
        {
            if (options.IsBackgroundDownload)
            {
                if (string.IsNullOrWhiteSpace(options.GameData.Name))
                    return default;

                var searchResult = dataSource.Search(options.GameData.Name, args.CancelToken);

                if (searchResult == null)
                    return default;

                var snc = new SortableNameConverter(new string[0], numberLength: 1, removeEditions: true);

                var nameToMatch = snc.Convert(options.GameData.Name).Deflate();

                var matchedGames = searchResult.Where(g => HasMatchingName(g, nameToMatch, snc) && platformUtility.PlatformsOverlap(requestPlatforms, g.Platforms)).ToList();

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
                        var searchResult = dataSource.Search(a, new System.Threading.CancellationToken());
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
            var gameNames = new List<string> { g.Title };
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

        public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).AgeRatings.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            return SelectImage(GameField.BackgroundImage, GetGameDetails(args).BackgroundOptions, "Select background");
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).CommunityScore;
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            return SelectImage(GameField.CoverImage, GetGameDetails(args).CoverOptions, "Select cover");
        }

        public override int? GetCriticScore(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).CriticScore;
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).Description;
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).Developers.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).Features.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).Genres.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override MetadataFile GetIcon(GetMetadataFieldArgs args)
        {
            return SelectImage(GameField.Icon, GetGameDetails(args).IconOptions, "Select icon");
        }

        public override ulong? GetInstallSize(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).InstallSize;
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            var gameDetails = GetGameDetails(args);
            var links = gameDetails.Links ?? new List<Link>();
            if (gameDetails.Url != null && !links.Any(l => l.Url == gameDetails.Url))
                links.Add(new Link(ProviderName, gameDetails.Url));

            return links.NullIfEmpty();
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).Names.FirstOrDefault();
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).Platforms.NullIfEmpty();
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).Publishers.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override IEnumerable<MetadataProperty> GetRegions(GetMetadataFieldArgs args)
        {
            return base.GetRegions(args);
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).ReleaseDate;
        }

        public override IEnumerable<MetadataProperty> GetSeries(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).Series.NullIfEmpty()?.Select(ToNameProperty);
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            return GetGameDetails(args).Tags.NullIfEmpty()?.Select(ToNameProperty);
        }

        protected virtual bool FilterImage(GameField field, IImageData imageData)
        {
            return true;
        }

        protected MetadataFile SelectImage(GameField field, List<IImageData> images, string caption)
        {
            images = images?.FindAll(img => FilterImage(field, img));

            if (images == null || images.Count == 0)
                return null;

            if (options.IsBackgroundDownload || images.Count == 1)
            {
                return new MetadataFile(images.First().Url);
            }
            else
            {
                var imageOptions = images?.Select(i => new ImgOption(i)).ToList<ImageFileOption>();
                var selected = playniteApi.Dialogs.ChooseImageFile(imageOptions, caption);
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
