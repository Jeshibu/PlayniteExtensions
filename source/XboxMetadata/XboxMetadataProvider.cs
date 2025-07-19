using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using XboxMetadata.Scrapers;

namespace XboxMetadata;

public class XboxMetadataProvider : OnDemandMetadataProvider
{
    private readonly ILogger logger = LogManager.GetLogger();
    private readonly MetadataRequestOptions options;
    private readonly XboxMetadataSettings settings;
    private readonly IPlayniteAPI playniteApi;
    private readonly ScraperManager scraperManager;
    private XboxGameDetails foundGame;

    public static List<MetadataField> Fields { get; } =
    [
        MetadataField.Name,
        MetadataField.Description,
        MetadataField.Developers,
        MetadataField.Publishers,
        MetadataField.CommunityScore,
        MetadataField.InstallSize,
        MetadataField.Genres,
        MetadataField.Features,
        MetadataField.Platform,
        MetadataField.CoverImage,
        MetadataField.BackgroundImage,
        MetadataField.ReleaseDate,
        MetadataField.AgeRating,
        MetadataField.Links,
    ];

    public override List<MetadataField> AvailableFields => Fields;

    public XboxMetadataProvider(MetadataRequestOptions options, XboxMetadataSettings settings, IPlayniteAPI playniteApi, ScraperManager scraperManager)
    {
        this.options = options;
        this.settings = settings;
        this.playniteApi = playniteApi;
        this.scraperManager = scraperManager;
    }

    public override string GetName(GetMetadataFieldArgs args)
    {
        return FindGame().Title;
    }

    public override string GetDescription(GetMetadataFieldArgs args)
    {
        var description = FindGame().Description;
        if (description == null)
            return null;

        return Regex.Replace(description, "\r?\n", "<br>$0");
    }

    public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
    {
        return FindGame().Developers.NullIfEmpty()?.Select(d=>new MetadataNameProperty(d));
    }

    public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
    {
        return FindGame().Publishers.NullIfEmpty()?.Select(d => new MetadataNameProperty(d));
    }

    public override int? GetCommunityScore(GetMetadataFieldArgs args)
    {
        return FindGame().CommunityScore;
    }

    public override ulong? GetInstallSize(GetMetadataFieldArgs args)
    {
        return FindGame().InstallSize;
    }

    public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
    {
        return FindGame().Genres.NullIfEmpty()?.Select(g=>new MetadataNameProperty(g));
    }

    public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
    {
        return FindGame().Features.NullIfEmpty()?.Select(f => new MetadataNameProperty(f));
    }

    public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
    {
        return FindGame().Platforms.NullIfEmpty();
    }

    public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
    {
        return PickImage("Select cover", FindGame().Covers, settings.Cover);
    }

    public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
    {
        return PickImage("Select background", FindGame().Backgrounds, settings.Background);
    }

    public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
    {
        var releaseDate = FindGame().ReleaseDate;
        if (!releaseDate.HasValue || releaseDate.Value == default)
            return null;

        return new ReleaseDate(releaseDate.Value);
    }

    public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
    {
        var rating = FindGame().AgeRating;
        if (rating == null || !RatingBoardMatchesSettings(rating))
            return null;

        return new[] { new MetadataNameProperty(rating) };
    }

    private bool RatingBoardMatchesSettings(string rating)
    {
        return playniteApi.ApplicationSettings.AgeRatingOrgPriority switch
        {
            AgeRatingOrg.ESRB => rating.StartsWith("ESRB"),
            AgeRatingOrg.PEGI => rating.StartsWith("PEGI"),
            _ => true,
        };
    }

    private static string ShortenRatingString(string longRatingName)
    {
        return longRatingName.ToUpper() switch
        {
            "RATING PENDING" => "RP",
            "ADULTS ONLY 18+" => "AO",
            "MATURE 17+" => "M",
            "TEEN" => "T",
            "EVERYONE 10+" => "E10+",
            "EVERYONE" => "E",
            _ => longRatingName,
        };
    }

    public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
    {
        return FindGame().Links;
    }

    private MetadataFile PickImage(string caption, List<ImageData> images, XboxImageSourceSettings imgSettings)
    {
        if (images == null || images.Count == 0)
            return null;

        Func<ImageData, bool> FilterImageBySize = i =>
        {
            bool biggerThanMinimum = i != null && i.Width >= imgSettings.MinWidth && i.Height >= imgSettings.MinHeight;
            if (!biggerThanMinimum)
                return false;

            return imgSettings.AspectRatio switch
            {
                AspectRatio.Vertical => i.Width < i.Height,
                AspectRatio.Horizontal => i.Width > i.Height,
                AspectRatio.Square => i.Width == i.Height,
                _ => true,
            };
        };
        var filteredImages = images
                             .Where(FilterImageBySize)
                             .ToDictionarySafe(i => i.Url).Values //deduplicate by Url - for old games BoxArt and Poster are the same
                             .Select(XboxImageFileOption.FromImageData)
                             .ToList<ImageFileOption>();

        if (filteredImages.Count == 0)
            return null;

        ImageFileOption selected;
        if (options.IsBackgroundDownload || filteredImages.Count == 1)
        {
            selected = filteredImages.First();
        }
        else
        {
            selected = playniteApi.Dialogs.ChooseImageFile(filteredImages, caption);
        }

        if (selected == null)
        {
            return null;
        }
        else
        {
            var selectedImage = ((XboxImageFileOption)selected).ImageData;
            return new MetadataFile(selectedImage.Url);
        }
    }

    private XboxGameDetails FindGame()
    {
        if (foundGame != null)
            return foundGame;

        try
        {
            XboxGameSearchResultItem foundSearchResult = null;
            if (options.IsBackgroundDownload)
            {
                foundSearchResult = scraperManager.Search(settings, options.GameData, options.GameData.Name, onlyExactMatches: true)?.FirstOrDefault();
            }
            else
            {
                var selected = playniteApi.Dialogs.ChooseItemWithSearch(null, a =>
                {
                    var searchResults = scraperManager.Search(settings, options.GameData, a);
                    return searchResults.Select(XboxGameSearchItemOption.FromSearchResult)?.ToList<GenericItemOption>() ?? [];
                }, options.GameData.Name, "Select Xbox game");

                if (selected != null)
                    foundSearchResult = ((XboxGameSearchItemOption)selected).Game;
            }
            if (foundSearchResult == null)
            {
                return foundGame = new XboxGameDetails();
            }
            else
            {
                return foundGame = scraperManager.GetDetails(settings, foundSearchResult);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error fetching Xbox data");
            playniteApi.Notifications.Add("xboxmetadata-fetch", "Error fetching Xbox metadata: " + ex.Message, NotificationType.Error);
            return foundGame = new XboxGameDetails();
        }
    }

    private class XboxGameSearchItemOption : GenericItemOption
    {
        public XboxGameSearchResultItem Game { get; set; }
        public static XboxGameSearchItemOption FromSearchResult(XboxGameSearchResultItem game)
        {
            var descriptionItems = new List<string>();
            if (game.ReleaseDate.HasValue)
                descriptionItems.Add($"{game.ReleaseDate.Value:d}");
            if (game.Platforms.Any())
                descriptionItems.Add(string.Join("/", game.Platforms));

            descriptionItems.Add(game.Id);

            return new XboxGameSearchItemOption { Name = game.Title, Description = string.Join(" | ", descriptionItems), Game = game };
        }
    }

    private class XboxImageFileOption : ImageFileOption
    {
        public XboxImageFileOption() : base() { }
        public XboxImageFileOption(string path) : base(path) { }

        public ImageData ImageData { get; set; }

        public static XboxImageFileOption FromImageData(ImageData imageData)
        {
            var o = new XboxImageFileOption(imageData.ThumbnailUrl ?? imageData.Url) { ImageData = imageData };
            return o;
        }
    }
}