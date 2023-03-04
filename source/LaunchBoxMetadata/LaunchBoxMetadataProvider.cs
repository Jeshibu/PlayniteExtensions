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
    public class LaunchBoxMetadataProvider : OnDemandMetadataProvider, IDisposable
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly MetadataRequestOptions options;
        private readonly LaunchBoxMetadata plugin;
        private readonly LaunchBoxMetadataSettings settings;
        private readonly LaunchBoxDatabase database;
        private readonly IPlatformUtility platformUtility;
        private LaunchBoxGame foundGame;
        private Dictionary<string, DownloadedImage> downloadedImages = new Dictionary<string, DownloadedImage>();
        private HttpClient httpClient = new HttpClient();

        private class DownloadedImage
        {
            public LaunchBoxGameImage Metadata { get; set; }
            public MagickImage Image { get; set; }
            public string Path { get; set; }
            public string Url { get => "https://images.launchbox-app.com/" + Metadata.FileName; }

            public DownloadedImage(LaunchBoxGameImage gi, MagickImage mi, string path)
            {
                Metadata = gi;
                Image = mi;
                Path = path;
            }
        }

        public override void Dispose()
        {
            httpClient.Dispose();
            foreach (var di in downloadedImages.Values)
            {
                try
                {
                    File.Delete(di.Path);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error deleting {di.Path}");
                }
            }
            base.Dispose();
        }

        public override List<MetadataField> AvailableFields => plugin.SupportedFields;

        public LaunchBoxMetadataProvider(MetadataRequestOptions options, LaunchBoxMetadata plugin, LaunchBoxMetadataSettings settings, LaunchBoxDatabase database, IPlatformUtility platformUtility)
        {
            this.options = options;
            this.plugin = plugin;
            this.settings = settings;
            this.database = database;
            this.platformUtility = platformUtility;
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

        private bool FilterImageSize(DownloadedImage di, LaunchBoxImageSourceSettings imgSetting)
        {
            if (di.Image.Width < imgSetting.MinWidth || di.Image.Height < imgSetting.MinHeight)
                return false;

            switch (imgSetting.AspectRatio)
            {
                case AspectRatio.Vertical:
                    return di.Image.Width < di.Image.Height;
                case AspectRatio.Horizontal:
                    return di.Image.Width > di.Image.Height;
                case AspectRatio.Square:
                    return di.Image.Width == di.Image.Height;
                case AspectRatio.Any:
                default:
                    return true;
            }
        }

        private List<string> GetImageTypeWhitelist(LaunchBoxImageSourceSettings imgSettings)
        {
            var whitelistedImgTypes = imgSettings.ImageTypes.Where(t => t.Checked).Select(t => t.Name).ToList();
            return whitelistedImgTypes;
        }

        private IEnumerable<DownloadedImage> DownloadImages(LaunchBoxImageSourceSettings imgSettings)
        {
            var whitelistedImgTypes = GetImageTypeWhitelist(imgSettings);

            var images = GetImages().Where(i => whitelistedImgTypes.Contains(i.Type)).ToList();

            var output = new List<DownloadedImage>();

            plugin.PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
            {
                var downloadTasks = images.Select(DownloadImage).ToArray();
                Task.WaitAll(downloadTasks, a.CancelToken);
                output.AddRange(downloadTasks.Where(t => t.Status == TaskStatus.RanToCompletion).Select(t => t.Result));
            }, new GlobalProgressOptions("Downloading images...", cancelable: true));
            return output;
        }

        private async Task<DownloadedImage> DownloadImage(LaunchBoxGameImage img)
        {
            if (downloadedImages.TryGetValue(img.FileName, out var downloaded))
                return downloaded;

            using (var stream = await httpClient.GetStreamAsync("https://images.launchbox-app.com/" + img.FileName))
            {
                var magickImage = new MagickImage(stream);
                var path = Path.GetTempFileName();
                await magickImage.WriteAsync(path);
                downloaded = new DownloadedImage(img, magickImage, path);
                downloadedImages.Add(img.FileName, downloaded);
                return downloaded;
            }
        }

        private MetadataFile PickImage(string caption, LaunchBoxImageSourceSettings imgSettings)
        {
            var whitelistedImgTypes = GetImageTypeWhitelist(imgSettings);
            var images = DownloadImages(imgSettings).Where(i => FilterImageSize(i, imgSettings))
                                          .OrderBy(i => whitelistedImgTypes.IndexOf(i.Metadata.Type))
                                          .Select(LaunchBoxImageFileOption.FromDownloadedImage)
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
                var downloadedImage = ((LaunchBoxImageFileOption)selected).DownloadedImage;
                return ScaleImage(downloadedImage, imgSettings);
            }
        }

        private MetadataFile ScaleImage(DownloadedImage img, LaunchBoxImageSourceSettings imgSettings)
        {
            double scaleX = img.Image.Width > imgSettings.MaxWidth ? (double)img.Image.Width / imgSettings.MaxWidth : 1;
            double scaleY = img.Image.Height > imgSettings.MaxHeight ? (double)img.Image.Height / imgSettings.MaxHeight : 1;
            double scale = Math.Min(scaleX, scaleY);
            if (scale == 1)
                return new MetadataFile(img.Url);

            logger.Info($"Scaling {img.Image.Width}x{img.Image.Height} image by {scale} to make it fit {imgSettings.MaxWidth}x{imgSettings.MaxHeight}");

            img.Image.Scale(new Percentage(scale * 100));
            var filename = Path.GetFileName(img.Url);
            return new MetadataFile(filename, img.Image.ToByteArray());
        }

        private class LaunchBoxImageFileOption : ImageFileOption
        {
            public LaunchBoxImageFileOption() : base() { }
            public LaunchBoxImageFileOption(string path) : base(path) { }

            public DownloadedImage DownloadedImage { get; set; }

            public static LaunchBoxImageFileOption FromDownloadedImage(DownloadedImage di)
            {
                var o = new LaunchBoxImageFileOption(di.Path) { Description = di.Metadata.Type, DownloadedImage = di };
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
            if (settings.UseWikipediaLink && !string.IsNullOrWhiteSpace(game.WikipediaURL))
                links.Add(new Link("Wikipedia", game.WikipediaURL));

            if (settings.UseVideoLink && !string.IsNullOrWhiteSpace(game.VideoURL))
                links.Add(new Link("Video", game.VideoURL));

            return links;
        }
    }
}