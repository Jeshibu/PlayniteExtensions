using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayniteExtensions.Common;
using Playnite.SDK.Models;
using System.Text.RegularExpressions;
using ImageMagick;
using System.Net.Http;
using System.IO;

namespace LaunchBoxMetadata
{
    public class LaunchBoxMetadataProvider : OnDemandMetadataProvider
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly MetadataRequestOptions options;
        private readonly LaunchBoxMetadata plugin;
        private readonly LaunchBoxMetadataSettings settings;
        private readonly LaunchBoxDatabase database;
        private readonly IPlatformUtility platformUtility;
        private readonly LaunchBoxWebscraper scraper;
        private LaunchBoxGame foundGame;
        private List<LaunchBoxImageDetails> foundImages;

        public override List<MetadataField> AvailableFields => plugin.SupportedFields;

        public LaunchBoxMetadataProvider(MetadataRequestOptions options, LaunchBoxMetadata plugin, LaunchBoxMetadataSettings settings, LaunchBoxDatabase database, IPlatformUtility platformUtility, LaunchBoxWebscraper scraper)
        {
            this.options = options;
            this.plugin = plugin;
            this.settings = settings;
            this.database = database;
            this.platformUtility = platformUtility;
            this.scraper = scraper;
        }

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
                            else if (platform is MetadataNameProperty namePlatform && platformNames.Contains(namePlatform.Name))
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

        private IEnumerable<LaunchBoxGameImage> GetImages()
        {
            var id = FindGame().DatabaseID;
            if (id == null)
                return new LaunchBoxGameImage[0];

            return database.GetGameImages(id);
        }

        private List<LaunchBoxImageDetails> GetImageDetails()
        {
            if (foundImages != null)
                return foundImages;

            var id = FindGame().DatabaseID;
            if (id == null)
                return foundImages = new List<LaunchBoxImageDetails>();

            return foundImages = scraper.GetGameImageDetails(id).ToList();
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

            return platformUtility.GetPlatforms(platform);
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

            return new[] { new MetadataNameProperty("ESRB " + esrbRating) };
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {

            return Split(FindGame().Genres) ?? base.GetGenres(args);
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            return Split(FindGame().Developer, StringExtensions.TrimCompanyForms) ?? base.GetDevelopers(args);
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            return Split(FindGame().Publisher, StringExtensions.TrimCompanyForms) ?? base.GetPublishers(args);
        }

        private bool FilterImage(LaunchBoxImageDetails imgDetails, ICollection<string> whitelistedTypes, LaunchBoxImageSourceSettings imgSetting)
        {
            if (!whitelistedTypes.Contains(imgDetails.Type))
                return false;

            if (imgDetails.Width < imgSetting.MinWidth || imgDetails.Height < imgSetting.MinHeight)
                return false;

            switch (imgSetting.AspectRatio)
            {
                case AspectRatio.Vertical:
                    return imgDetails.Width < imgDetails.Height;
                case AspectRatio.Horizontal:
                    return imgDetails.Width > imgDetails.Height;
                case AspectRatio.Square:
                    return imgDetails.Width == imgDetails.Height;
                case AspectRatio.Any:
                default:
                    return true;
            }
        }

        private MetadataFile PickImage(string caption, LaunchBoxImageSourceSettings imgSettings)
        {
            var whitelistedImgTypes = imgSettings.ImageTypes.Where(t => t.Checked).Select(t => t.Name).ToList();

            var images = GetImageDetails().Where(i => FilterImage(i, whitelistedImgTypes, imgSettings))
                                          .OrderBy(i => whitelistedImgTypes.IndexOf(i.Type))
                                          .Select(LaunchBoxImageFileOption.FromImageDetails)
                                          .ToList<ImageFileOption>();
            if (images.Count == 0)
                return null;

            ImageFileOption selected;
            if (options.IsBackgroundDownload || images.Count == 1)
            {
                selected = images.FirstOrDefault();
            }
            else
            {
                selected = plugin.PlayniteApi.Dialogs.ChooseImageFile(images, caption);
            }

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

        private async Task<MetadataFile> ScaleImageAsync(LaunchBoxImageDetails imgDetails, LaunchBoxImageSourceSettings imgSettings)
        {
            double scaleX = imgDetails.Width > imgSettings.MaxWidth ? (double)imgDetails.Width / imgSettings.MaxWidth : 1;
            double scaleY = imgDetails.Height > imgSettings.MaxHeight ? (double)imgDetails.Height / imgSettings.MaxHeight : 1;
            double scale = Math.Min(scaleX, scaleY);
            if (scale == 1)
                return new MetadataFile(imgDetails.Url);

            logger.Info($"Scaling {imgDetails.Width}x{imgDetails.Height} image by {scale} to make it fit {imgSettings.MaxWidth}x{imgSettings.MaxHeight}");

            using (HttpClient client = new HttpClient())
            using (var stream = await client.GetStreamAsync(imgDetails.Url))
            {
                MagickImage img = new MagickImage(stream);
                img.Scale(new Percentage(scale * 100));
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
                links.Add(new Link("LaunchBox Games Database", "https://gamesdb.launchbox-app.com/games/details/" + game.DatabaseID));

            if (settings.UseWikipediaLink && !string.IsNullOrWhiteSpace(game.WikipediaURL))
                links.Add(new Link("Wikipedia", game.WikipediaURL));

            if (settings.UseVideoLink && !string.IsNullOrWhiteSpace(game.VideoURL))
                links.Add(new Link("Video", game.VideoURL));

            return links;
        }
    }
}