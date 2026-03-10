using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using PlayniteExtensions.Common;
using System.Threading;

namespace PlayniteExtensions.Metadata.Common;

public abstract class GenericMetadataProvider<TSearchResult>(IGameSearchProvider<TSearchResult> dataSource, MetadataRequestOptions options, IPlayniteAPI playniteApi, IPlatformUtility platformUtility) : OnDemandMetadataProvider where TSearchResult : IGameSearchResult
{
    protected readonly IGameSearchProvider<TSearchResult> DataSource = dataSource;
    protected readonly MetadataRequestOptions Options = options;
    protected readonly List<Platform> RequestPlatforms = options.GameData.Platforms;
    protected readonly IPlayniteAPI PlayniteApi = playniteApi;
    protected readonly IPlatformUtility PlatformUtility = platformUtility;
    protected readonly ILogger Logger = LogManager.GetLogger();
    protected GameDetails FoundGame = null;
    protected abstract string ProviderName { get; }

    protected GameDetails GetGameDetails(GetMetadataFieldArgs args)
    {
        if (FoundGame != null)
            return FoundGame;

        if (Options.IsBackgroundDownload && DataSource.TryGetDetails(Options.GameData, out var details, args.CancelToken))
            return FoundGame = details;

        var searchResult = GetSearchResultGame(args);
        if (searchResult != null)
            return FoundGame = DataSource.GetDetails(searchResult, searchGame: Options.GameData);

        return FoundGame = new GameDetails();
    }

    public TSearchResult GetSearchResultGame(GetMetadataFieldArgs args)
    {
        if (Options.IsBackgroundDownload)
        {
            if (string.IsNullOrWhiteSpace(Options.GameData.Name))
                return default;

            var searchResult = DataSource.Search(Options.GameData.Name, args.CancelToken);

            if (searchResult == null)
                return default;

            var snc = new SortableNameConverter([], numberLength: 1);

            var nameToMatch = snc.Convert(Options.GameData.Name, removeEditions: true).Deflate();

            var matchedGames = searchResult.Where(g => HasMatchingName(g, nameToMatch, snc) && PlatformUtility.PlatformsOverlap(RequestPlatforms, g.Platforms)).ToList();

            switch (matchedGames.Count)
            {
                case 0:
                    return default;
                case 1:
                    return matchedGames.First();
                default:
                    var searchReleaseDate = Options.GameData.ReleaseDate;
                    var sortedByReleaseDateProximity = matchedGames.OrderBy(g => GetDaysApart(searchReleaseDate, g.ReleaseDate)).ToList();
                    return sortedByReleaseDateProximity.First();
            }
        }
        else
        {
            var selectedGame = PlayniteApi.Dialogs.ChooseItemWithSearch(null, (a) =>
            {
                var searchOutput = new List<GenericItemOption>();

                if (string.IsNullOrWhiteSpace(a))
                    return searchOutput;

                try
                {
                    var searchResult = DataSource.Search(a, CancellationToken.None);
                    if (searchResult != null)
                        searchOutput.AddRange(searchResult.Select(DataSource.ToGenericItemOption));
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Failed to get {ProviderName} search data for <{a}>");
                }

                return searchOutput;
            }, Options.GameData.Name, string.Empty);

            var selectedGameReal = (GenericItemOption<TSearchResult>)selectedGame;
            return selectedGameReal == null ? default : selectedGameReal.Item;
        }
    }

    private static int GetDaysApart(ReleaseDate? searchDate, ReleaseDate? resultDate)
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
            var deflatedGameName = snc.Convert(gameName, removeEditions: true).Deflate();
            if (deflatedSearchName.Equals(deflatedGameName, StringComparison.InvariantCultureIgnoreCase))
                return true;
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
        return NullIfZero(GetGameDetails(args).CommunityScore);
    }

    public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
    {
        return SelectImage(GameField.CoverImage, GetGameDetails(args).CoverOptions, "Select cover");
    }

    public override int? GetCriticScore(GetMetadataFieldArgs args)
    {
        return NullIfZero(GetGameDetails(args).CriticScore);
    }

    private static int? NullIfZero(int? number) => number == 0 ? null : number;

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
        var installSize = GetGameDetails(args).InstallSize;
        return installSize == 0 ? null : installSize;
    }

    public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
    {
        var gameDetails = GetGameDetails(args);
        var links = gameDetails.Links ?? [];
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

        if (Options.IsBackgroundDownload || images.Count == 1)
            return new(images.First().Url);

        var imageOptions = images.Select(i => new ImgOption(i)).ToList<ImageFileOption>();
        var selected = PlayniteApi.Dialogs.ChooseImageFile(imageOptions, caption);
        var fullSizeUrl = (selected as ImgOption)?.Image.Url;

        if (fullSizeUrl == null)
            return null;

        return new(fullSizeUrl);
    }

    protected class ImgOption : ImageFileOption
    {
        public ImgOption(IImageData image)
        {
            Image = image;
            Path = image.ThumbnailUrl ?? image.Url;
            if (image.Width != 0 && image.Height != 0)
                Description = $"{image.Width}x{image.Height} {image.Description}";
        }

        public IImageData Image { get; }
    }

    protected static MetadataProperty ToNameProperty(string name)
    {
        return new MetadataNameProperty(name);
    }
}
