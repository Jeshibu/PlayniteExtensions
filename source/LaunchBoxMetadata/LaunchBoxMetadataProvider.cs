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

public class LaunchBoxMetadataProvider(MetadataRequestOptions options, LaunchBoxMetadata plugin, LaunchBoxMetadataSettings settings, LaunchBoxDatabase database, IPlatformUtility platformUtility, LaunchBoxWebscraper scraper) : OnDemandMetadataProvider
{
    private readonly ILogger logger = LogManager.GetLogger();
    private LaunchBoxGame foundGame;
    private string foundGameUrl;
    private List<LaunchBoxImageDetails> foundImages;
    private TitleComparer titleComparer = new();

    public override List<MetadataField> AvailableFields => plugin.SupportedFields;

    private LaunchBoxGame FindGame()
    {
        if (foundGame != null)
            return foundGame;

        if (options.IsBackgroundDownload)
        {
            var results = database.SearchGames(options.GameData.Name, 100);
            var deflatedSearchGameName = options.GameData.Name.Deflate();
            var platformSpecs = options.GameData.Platforms?.Where(p => p.SpecificationId != null).Select(p => p.SpecificationId).ToList();
            var platformNames = options.GameData.Platforms?.Select(p => p.Name).ToList();
            foreach (var game in results)
            {
                var deflatedMatchedGameName = game.MatchedName.Deflate();
                if (!deflatedSearchGameName.Equals(deflatedMatchedGameName, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (options.GameData.Platforms?.Count > 0)
                {
                    var platforms = platformUtility.GetPlatforms(game.Platform);
                    foreach (var platform in platforms)
                    {
                        if (platform is MetadataSpecProperty specPlatform && platformSpecs.Contains(specPlatform.Id))
                        {
                            return foundGame = game;
                        }
                        else if (platform is MetadataNameProperty namePlatform && platformNames.Contains(namePlatform.Name, titleComparer))
                        {
                            return foundGame = game;
                        }
                    }
                }
                else
                {
                    //no platforms to match, so consider a name match a success
                    return foundGame = game;
                }
            }
            return foundGame = new LaunchBoxGame();
        }
        else
        {
            var chosen = plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(null, s =>
            {
                var results = database.SearchGames(s).Select(LaunchBoxGameItemOption.FromLaunchBoxGame).ToList<GenericItemOption>();
                return results;
            }, options.GameData.Name, "LaunchBox: select game");
            if (chosen == null)
                return foundGame = new LaunchBoxGame();
            else
                return foundGame = ((LaunchBoxGameItemOption)chosen).Game;
        }
    }

    private string GetLaunchBoxGamesDatabaseUrl(LaunchBoxGame game = null)
    {
        game = game ?? FindGame();
        if (game.DatabaseID == null)
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
        if (id == null)
            return foundImages = [];

        var detailsUrl = GetLaunchBoxGamesDatabaseUrl(game);
        if (string.IsNullOrWhiteSpace(detailsUrl))
        {
            logger.Error($"Could not retrieve website ID for database ID {id}");
            return foundImages = [];
        }

        return foundImages = scraper.GetGameImageDetails(detailsUrl).ToList();
    }

    private class LaunchBoxGameItemOption : GenericItemOption
    {
        public LaunchboxGameSearchResult Game { get; set; }

        public static LaunchBoxGameItemOption FromLaunchBoxGame(LaunchboxGameSearchResult g)
        {
            var name = g.Name;
            if (g.MatchedName != g.Name)
                name += $" ({g.MatchedName})";

            var descriptionItems = new List<string>();
            if (g.ReleaseDate.HasValue)
                descriptionItems.Add(g.ReleaseDate.Value.ToString("yyyy-MM-dd"));
            if (g.ReleaseYear != 0)
                descriptionItems.Add(g.ReleaseYear.ToString());
            descriptionItems.Add(g.Platform);

            string description = string.Join(" | ", descriptionItems);

            return new LaunchBoxGameItemOption
            {
                Game = g,
                Name = name,
                Description = description,
            };
        }
    }

    private IEnumerable<MetadataProperty> Split(string str, Func<string, string> stringSelector = null)
    {
        if (string.IsNullOrWhiteSpace(str))
            return null;

        var split = str.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        var strings = stringSelector == null ? split : split.Select(stringSelector);
        var output = strings.Select(g => new MetadataNameProperty(g.Trim()));

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

    private bool FilterImage(LaunchBoxImageDetails imgDetails, ICollection<string> whitelistedTypes, ICollection<string> whitelistedRegions, LaunchBoxImageSourceSettings imgSetting)
    {
        if (!whitelistedTypes.Contains(imgDetails.Type))
            return false;

        if (!whitelistedRegions.Contains(imgDetails.Region))
            return false;

        if (imgDetails.Width < imgSetting.MinWidth || imgDetails.Height < imgSetting.MinHeight)
            return false;

        return imgSetting.AspectRatio switch
        {
            AspectRatio.Vertical => imgDetails.Width < imgDetails.Height,
            AspectRatio.Horizontal => imgDetails.Width > imgDetails.Height,
            AspectRatio.Square => imgDetails.Width == imgDetails.Height,
            _ => true,
        };
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
        {
            return null;
        }
        else
        {
            var selectedImageDetails = ((LaunchBoxImageFileOption)selected).ImageDetails;
            var task = ScaleImageAsync(selectedImageDetails, imgSettings);
            task.Wait();
            return task.Result;
        }
    }

    private List<ImageFileOption> GetImageOptions(LaunchBoxImageSourceSettings imgSettings)
    {
        var whitelistedImgTypes = imgSettings.ImageTypes.Where(t => t.Checked).Select(t => t.Name).ToList();
        var whitelistedRegions = GetWhitelistedRegions();

        var images = GetImageDetails().Where(i => FilterImage(i, whitelistedImgTypes, whitelistedRegions, imgSettings)).ToList();

        return images.OrderBy(i => whitelistedImgTypes.IndexOf(i.Type))
                     .ThenBy(i => whitelistedRegions.IndexOf(i.Region))
                     .ThenByDescending(i => i.Width * i.Height)
                     .Select(LaunchBoxImageFileOption.FromImageDetails)
                     .ToList<ImageFileOption>();
    }

    private List<string> GetWhitelistedRegions()
    {
        var comparer = StringComparer.InvariantCultureIgnoreCase;
        var gameRegions = options.GameData?.Regions?.Select(r => r.Name).ToList();
        if (settings.PreferGameRegion && gameRegions != null && gameRegions.Any())
        {
            var output = new List<string>();
            foreach (var regionSetting in settings.Regions)
            {
                var aliases = regionSetting.Aliases?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                if (gameRegions.Any(gr => comparer.Equals(gr, regionSetting.Name) || (aliases != null && aliases.ContainsString(gr))))
                    output.Add(regionSetting.Name); //put any matched region at the top
            }
            foreach (var regionSetting in settings.Regions)
            {
                if(regionSetting.Checked && !output.Contains(regionSetting.Name))
                    output.Add(regionSetting.Name); //add the rest of the enabled regions
            }
            return output;
        }
        else
        {
            return settings.Regions.Where(r => r.Checked).Select(r => r.Name).ToList();
        }
    }

    private async Task<MetadataFile> ScaleImageAsync(LaunchBoxImageDetails imgDetails, LaunchBoxImageSourceSettings imgSettings)
    {
        bool scaleDown = imgDetails.Width > imgSettings.MaxWidth
                         || imgDetails.Height > imgSettings.MaxHeight;

        bool resizeToSquare = imgSettings.AspectRatio == AspectRatio.AnyExtendToSquare
                              && imgDetails.Width != imgDetails.Height;

        if (!scaleDown && !resizeToSquare)
            return new MetadataFile(imgDetails.Url);

        using (HttpClient client = new())
        using (var stream = await client.GetStreamAsync(imgDetails.Url))
        {
            int maxWidth = imgSettings.MaxWidth;
            int maxHeight = imgSettings.MaxHeight;
            int minSize = Math.Min(imgSettings.MaxWidth, imgSettings.MaxHeight);

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
    }

    private class LaunchBoxImageFileOption : ImageFileOption
    {
        public LaunchBoxImageFileOption() : base() { }
        public LaunchBoxImageFileOption(string path) : base(path) { }

        public LaunchBoxImageDetails ImageDetails { get; set; }

        public static LaunchBoxImageFileOption FromImageDetails(LaunchBoxImageDetails imageDetails)
        {
            var o = new LaunchBoxImageFileOption(imageDetails.ThumbnailUrl) { Description = imageDetails.Type, ImageDetails = imageDetails };
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
        if (game.DatabaseID == null)
            return base.GetLinks(args);

        var links = new List<Link>();
        if (settings.UseLaunchBoxLink)
        {
            string gameUrl = GetLaunchBoxGamesDatabaseUrl(game);
            if (!string.IsNullOrEmpty(gameUrl))
                links.Add(new Link("LaunchBox Games Database", gameUrl));
        }

        if (settings.UseWikipediaLink && !string.IsNullOrWhiteSpace(game.WikipediaURL))
            links.Add(new Link("Wikipedia", game.WikipediaURL));

        if (settings.UseVideoLink && !string.IsNullOrWhiteSpace(game.VideoURL))
            links.Add(new Link("Video", game.VideoURL));

        return links.NullIfEmpty();
    }
}