using ImageMagick;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LaunchBoxMetadata;

public class LaunchBoxMetadataProvider(MetadataRequestOptions options, LaunchBoxMetadata plugin, LaunchBoxMetadataSettings settings, LaunchBoxDatabase database, IPlatformUtility platformUtility, LaunchBoxWebScraper scraper) : OnDemandMetadataProvider
{
    private readonly ILogger logger = LogManager.GetLogger();
    private LaunchBoxGame foundGame;
    private string foundGameUrl;
    private List<LaunchBoxImageDetails> foundImages;

    public override List<MetadataField> AvailableFields => plugin.SupportedFields;

    private LaunchBoxGame FindGame()
    {
        return foundGame ??= (options.IsBackgroundDownload
            ? LaunchBoxHelper.FindGameInBackground(database, options.GameData, platformUtility)
            : LaunchBoxHelper.FindGameViaSearch(database, options.GameData));
    }

    private string GetLaunchBoxGamesDatabaseUrl(LaunchBoxGame game)
    {
        game ??= FindGame();
        if (game.DatabaseID == default)
            return null;

        string gameUrl = foundGameUrl ?? (foundGameUrl = scraper.GetLaunchBoxGamesDatabaseUrl(game.DatabaseID)) ?? (foundGameUrl = string.Empty);
        return gameUrl;
    }

    private List<LaunchBoxImageDetails> GetImageDetails()
    {
        if (foundImages != null)
            return foundImages;

        var game = FindGame();
        var id = game.DatabaseID;
        if (id == default)
            return foundImages = [];

        var detailsUrl = GetLaunchBoxGamesDatabaseUrl(game);

        return foundImages = LaunchBoxHelper.GetImageDetails(scraper, detailsUrl, id).ToList();
    }

    private IEnumerable<MetadataProperty> Split(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return null;

        var split = str.Split([';'], StringSplitOptions.RemoveEmptyEntries);
        var output = split.Select(g => new MetadataNameProperty(g.Trim()));

        return output;
    }

    private static IEnumerable<MetadataProperty> SplitCompanies(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return null;

        return str.SplitCompanies().Select(c => new MetadataNameProperty(c));
    }

    public override string GetName(GetMetadataFieldArgs args)
    {
        return FindGame().Name ?? base.GetName(args);
    }

    public override string GetDescription(GetMetadataFieldArgs args)
    {
        var overview = FindGame().Overview;
        if (overview == null)
            return base.GetDescription(args);

        overview = Regex.Replace(overview, "\r?\n", "<br>$0");
        return overview;
    }

    public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
    {
        string platform = FindGame().Platform;
        if (platform == null)
            return base.GetPlatforms(args);

        return platformUtility.GetPlatforms(platform).ToList().NullIfEmpty();
    }

    public override int? GetCommunityScore(GetMetadataFieldArgs args)
    {
        var commScore = FindGame().CommunityRating;
        if (commScore == 0)
            return base.GetCommunityScore(args);

        return (int)(commScore * 20); //from 0-5 to 0-100 range
    }

    public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
    {
        var game = FindGame();

        if (game.ReleaseDate.HasValue)
            return new ReleaseDate(game.ReleaseDate.Value);

        if (game.ReleaseYear != 0)
            return new ReleaseDate(game.ReleaseYear);

        return base.GetReleaseDate(args);
    }

    public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
    {
        var esrbRating = FindGame().ESRB;
        if (string.IsNullOrEmpty(esrbRating))
            return base.GetAgeRatings(args);

        if (esrbRating != "Not Rated")
        {
            var split = esrbRating.Split(' ');
            esrbRating = split[0];
        }

        return [new MetadataNameProperty("ESRB " + esrbRating)];
    }

    public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
    {
        return Split(FindGame().Genres) ?? base.GetGenres(args);
    }

    public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
    {
        return SplitCompanies(FindGame().Developer);
    }

    public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
    {
        return SplitCompanies(FindGame().Publisher);
    }

    private MetadataFile PickImage(string caption, LaunchBoxImageSourceSettings imgSettings)
    {
        var images = GetImageOptions(imgSettings);

        if (images.Count == 0)
            return null;

        ImageFileOption selected;
        if (options.IsBackgroundDownload || images.Count == 1)
            selected = images.First();
        else
            selected = plugin.PlayniteApi.Dialogs.ChooseImageFile(images, caption);

        if (selected == null)
            return null;

        var selectedImageDetails = ((LaunchBoxImageFileOption)selected).ImageDetails;
        return Task.Run(async () => await ScaleImageAsync(selectedImageDetails, imgSettings)).GetAwaiter().GetResult();
    }

    private List<ImageFileOption> GetImageOptions(LaunchBoxImageSourceSettings imgSettings)
    {
        var whitelistedImgTypes = imgSettings.ImageTypes.Where(t => t.Checked).Select(t => t.Name).ToList();
        var whitelistedRegions = LaunchBoxHelper.GetWhitelistedRegions(options.GameData?.Regions, settings);

        var images = GetImageDetails().Where(i => LaunchBoxHelper.FilterImage(i, whitelistedImgTypes, whitelistedRegions, imgSettings)).ToList();

        return images.OrderBy(i => whitelistedImgTypes.IndexOf(i.Type))
                     .ThenBy(i => whitelistedRegions.IndexOf(i.Region))
                     .ThenByDescending(i => i.Width * i.Height)
                     .Select(LaunchBoxImageFileOption.FromImageDetails)
                     .ToList<ImageFileOption>();
    }

    private async Task<MetadataFile> ScaleImageAsync(LaunchBoxImageDetails imgDetails, LaunchBoxImageSourceSettings imgSettings)
    {
        bool scaleDown = imgDetails.Width > imgSettings.MaxWidth
                         || imgDetails.Height > imgSettings.MaxHeight;

        bool resizeToSquare = imgSettings.AspectRatio == AspectRatio.AnyExtendToSquare
                              && imgDetails.Width != imgDetails.Height;

        if (!scaleDown && !resizeToSquare)
            return new MetadataFile(imgDetails.Url);

        using HttpClient client = new();
        using var stream = await client.GetStreamAsync(imgDetails.Url);
        uint maxWidth = imgSettings.MaxWidth;
        uint maxHeight = imgSettings.MaxHeight;
        uint minSize = Math.Min(imgSettings.MaxWidth, imgSettings.MaxHeight);

        if (imgSettings.AspectRatio == AspectRatio.AnyExtendToSquare)
            maxWidth = maxHeight = minSize;

        MagickImage img = new(stream);
        if (scaleDown)
        {
            logger.Info($"Scaling {imgDetails.Url} ({imgDetails.Width}x{imgDetails.Height}) to make it fit {maxWidth}x{maxHeight}");
            img.Scale(maxWidth, maxHeight);
        }

        if (imgSettings.AspectRatio == AspectRatio.AnyExtendToSquare && img.Width != img.Height)
        {
            logger.Info($"Extending {imgDetails.Url} ({img.Width}x{img.Height}) to make it {maxWidth}x{maxHeight}");

            img.BackgroundColor = MagickColor.FromRgba(0, 0, 0, 0);
            img.Extent(minSize, minSize, Gravity.Center);
        }

        var filename = Path.GetFileName(imgDetails.Url);
        return new MetadataFile(filename, img.ToByteArray());
    }

    private class LaunchBoxImageFileOption(string path) : ImageFileOption(path)
    {
        public LaunchBoxImageDetails ImageDetails { get; set; }

        public static LaunchBoxImageFileOption FromImageDetails(LaunchBoxImageDetails imageDetails)
        {
            var o = new LaunchBoxImageFileOption(imageDetails.ThumbnailUrl)
            {
                Description = $"{imageDetails.Width}x{imageDetails.Height} {imageDetails.Type}",
                ImageDetails = imageDetails,
            };
            return o;
        }
    }

    public override MetadataFile GetIcon(GetMetadataFieldArgs args)
    {
        return PickImage("Select icon", settings.Icon);
    }

    public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
    {
        return PickImage("Select cover", settings.Cover);
    }

    public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
    {
        return PickImage("Select background", settings.Background);
    }

    public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
    {
        var game = FindGame();
        if (game.DatabaseID == default)
            return base.GetLinks(args);

        var links = new List<Link>();

        void AddLink(string name, bool setting, Func<LaunchBoxGame, string> urlSelector)
        {
            if (!setting) return;
            var url = urlSelector(game);
            if (string.IsNullOrWhiteSpace(url)) return;
            links.Add(new(name, url));
        }

        AddLink("LaunchBox", settings.UseLaunchBoxLink, GetLaunchBoxGamesDatabaseUrl);
        AddLink("Wikipedia", settings.UseWikipediaLink, g => g.WikipediaURL);
        AddLink("Video", settings.UseVideoLink, g => g.VideoURL);

        return links.NullIfEmpty();
    }
}
