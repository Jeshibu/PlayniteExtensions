using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XboxMetadata
{
    public class XboxMetadataProvider : OnDemandMetadataProvider
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly MetadataRequestOptions options;
        private readonly XboxMetadataSettings settings;
        private readonly IPlayniteAPI playniteApi;
        private readonly IXboxScraper scraper;
        private readonly IPlatformUtility platformUtility;
        private XboxGameDetailsProductSummary foundGame;

        public static List<MetadataField> Fields { get; } = new List<MetadataField>
        {
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
        };

        public override List<MetadataField> AvailableFields => Fields;

        public XboxMetadataProvider(MetadataRequestOptions options, XboxMetadataSettings settings, IPlayniteAPI playniteApi, IXboxScraper scraper, IPlatformUtility platformUtility)
        {
            this.options = options;
            this.settings = settings;
            this.playniteApi = playniteApi;
            this.scraper = scraper;
            this.platformUtility = platformUtility;
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            var name = FindGame().Title;
            if (name == null)
                return null;

            platformUtility.GetPlatformsFromName(name, out string trimmedName);

            return trimmedName;
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
            return GetCompanies(FindGame().DeveloperName);
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            return GetCompanies(FindGame().PublisherName);
        }

        private IEnumerable<MetadataProperty> GetCompanies(string name)
        {
            var names = name?.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return names.NullIfEmpty()?.Select(n => new MetadataNameProperty(n.Trim().TrimCompanyForms()));
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            var rating = FindGame().AverageRating;
            if (rating == null)
                return null;

            return (int)(rating.Value * 20);
        }

        public override ulong? GetInstallSize(GetMetadataFieldArgs args)
        {
            var installSize = FindGame().MaxInstallSize;
            if (installSize == default)
                return null;

            return installSize;
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            var categories = FindGame().Categories;
            if (categories == null || categories.Length == 0)
                return null;

            return categories.Select(c => new MetadataNameProperty(c));
        }

        public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
        {
            var game = FindGame();
            if (game.Title == null) //if the game is an empty instance to stop constant scraping retries
                return null;

            var features = new List<string>();

            if (settings.ImportAccessibilityFeatures && game.AccessibilityCapabilities != null)
            {
                features.AddRange(game.AccessibilityCapabilities?.Audio.Select(c => "Accessibility: Audio: " + c));
                features.AddRange(game.AccessibilityCapabilities?.Gameplay.Select(c => "Accessibility: Gameplay: " + c));
                features.AddRange(game.AccessibilityCapabilities?.Input.Select(c => "Accessibility: Input: " + c));
                features.AddRange(game.AccessibilityCapabilities?.Visual.Select(c => "Accessibility: Visual: " + c));
            }

            if (game.Capabilities != null)
                features.AddRange(game.Capabilities.Values);

            Regex multiSpace = new Regex(@"\s{2,}");

            return features.NullIfEmpty()?.OrderBy(f => f).Select(f => new MetadataNameProperty(multiSpace.Replace(f, " ")));
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            var jsonPlatforms = FindGame().AvailableOn;
            if (jsonPlatforms == null || jsonPlatforms.Length == 0)
                return null;

            var output = jsonPlatforms.SelectMany(p => platformUtility.GetPlatforms(p)).ToList();

            return output.NullIfEmpty();
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            return PickImage("Select cover", settings.Cover);
        }

        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            return PickImage("Select background", settings.Background);
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            var releaseDate = FindGame().ReleaseDate;
            if (releaseDate == default)
                return null;

            return new ReleaseDate(releaseDate);
        }

        public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
        {
            var rating = FindGame().Rating;
            if (rating == null || !RatingBoardMatchesSettings(rating))
                return null;

            string shortRating = ShortenRatingString(rating.Rating);

            return new[] { new MetadataNameProperty($"{rating.BoardName} {shortRating}") };
        }

        private bool RatingBoardMatchesSettings(XboxGameDetailsAgeRating rating)
        {
            switch (playniteApi.ApplicationSettings.AgeRatingOrgPriority)
            {
                case AgeRatingOrg.ESRB: return rating.BoardName == "ESRB";
                case AgeRatingOrg.PEGI: return rating.BoardName == "PEGI";
                default: return true;
            }
        }

        private static string ShortenRatingString(string longRatingName)
        {
            switch (longRatingName.ToUpper())
            {
                case "RATING PENDING": return "RP";
                case "ADULTS ONLY 18+": return "AO";
                case "MATURE 17+": return "M";
                case "TEEN": return "T";
                case "EVERYONE 10+": return "E10+";
                case "EVERYONE": return "E";
                default: return longRatingName;
            }
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            var game = FindGame();
            if (game.Title == null)
                return null;

            var links = new List<Link>();
            if (settings.ImportAccessibilityFeatures && game.AccessibilityCapabilities?.PublisherInformationUri != null)
                links.Add(new Link("Accessibility information", game.AccessibilityCapabilities.PublisherInformationUri));

            links.Add(new Link("Xbox Store Page", scraper.GetStoreUrl(game.ProductId)));
            return links;
        }

        private MetadataFile PickImage(string caption, XboxImageSourceSettings imgSettings)
        {
            var images = FindGame().Images;
            if (images == null)
                return null;

            var potentialImages = new List<XboxImageDetails>();
            foreach (var fieldSetting in imgSettings.Fields)
            {
                if (!fieldSetting.Checked)
                    continue;

                switch (fieldSetting.Field)
                {
                    case ImageSourceField.BoxArt:
                        potentialImages.Add(images.BoxArt);
                        break;
                    case ImageSourceField.Poster:
                        potentialImages.Add(images.Poster);
                        break;
                    case ImageSourceField.SuperHeroArt:
                        potentialImages.Add(images.SuperHeroArt);
                        break;
                    case ImageSourceField.Screenshots:
                        potentialImages.AddRange(images.Screenshots);
                        break;
                    default:
                        continue;
                }
            }

            Func<XboxImageDetails, bool> FilterImageBySize = i =>
            {
                bool biggerThanMinimum = i!= null && i.Width > imgSettings.MinWidth && i.Height > imgSettings.MinHeight;
                if (!biggerThanMinimum)
                    return false;

                switch (imgSettings.AspectRatio)
                {
                    case AspectRatio.Vertical:
                        return i.Width < i.Height;
                    case AspectRatio.Horizontal:
                        return i.Width > i.Height;
                    case AspectRatio.Square:
                        return i.Width == i.Height;
                    case AspectRatio.Any:
                    default:
                        return true;
                }
            };
            var filteredImages = potentialImages
                                 .Where(FilterImageBySize)
                                 .ToDictionarySafe(i => i.Url).Values //deduplicate by Url - for old games BoxArt and Poster are the same
                                 .Select(XboxImageFileOption.FromImageDetails)
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
                var selectedImageDetails = ((XboxImageFileOption)selected).ImageDetails;
                string url;
                if (selectedImageDetails.Width > imgSettings.MaxWidth || selectedImageDetails.Height > imgSettings.MaxHeight)
                {
                    var width = Math.Min(selectedImageDetails.Width, imgSettings.MaxWidth);
                    var height = Math.Min(selectedImageDetails.Height, imgSettings.MaxHeight);
                    url = selectedImageDetails.GetResizedUrl(width, height, 100);
                }
                else
                {
                    url = selectedImageDetails.Url;
                }
                return new MetadataFile(url);
            }
        }

        private XboxGameDetailsProductSummary FindGame()
        {
            if (foundGame != null)
                return foundGame;

            try
            {
                XboxSearchResultGame foundSearchResult = null;
                if (options.IsBackgroundDownload)
                {
                    foundSearchResult = FindBestMatch();
                }
                else
                {
                    var selected = playniteApi.Dialogs.ChooseItemWithSearch(null, a =>
                    {
                        return scraper.Search(a).Select(XboxGameSearchItemOption.FromSearchResult).ToList<GenericItemOption>();
                    }, options.GameData.Name, "Select Xbox game");

                    if (selected != null)
                        foundSearchResult = ((XboxGameSearchItemOption)selected).Game;
                }
                if (foundSearchResult == null)
                    return foundGame = new XboxGameDetailsProductSummary();
                else
                    return foundGame = scraper.GetGameDetails(foundSearchResult.Id);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Xbox data");
                playniteApi.Notifications.Add("xboxmetadata-fetch", "Error fetching Xbox metadata: " + ex.Message, NotificationType.Error);
                return foundGame = new XboxGameDetailsProductSummary();
            }
        }

        private XboxSearchResultGame FindBestMatch()
        {
            var snc = new SortableNameConverter(new[] { "the", "a", "an" }, removeEditions: true);
            var searchNameNormalized = snc.Convert(options.GameData.Name).Deflate();
            var searchResults = scraper.Search(options.GameData.Name);
            foreach (var sr in searchResults)
            {
                var platforms = platformUtility.GetPlatformsFromName(sr.Title, out string nameTrimmed).ToList();
                if (platforms.Any() && options.GameData.Platforms.Any())
                {
                    bool matched = false;
                    foreach (var mp in platforms)
                    {
                        foreach (var platform in options.GameData.Platforms)
                        {
                            if (mp is MetadataSpecProperty specPlatform)
                                matched |= specPlatform.Id == platform.SpecificationId;
                            else if (mp is MetadataNameProperty namePlatform)
                                matched |= namePlatform.Name == platform.Name;
                        }
                    }
                    if (!matched)
                        continue;
                }
                var searchResultNameNormalized = snc.Convert(nameTrimmed).Deflate();
                if (searchNameNormalized.Equals(searchNameNormalized, StringComparison.InvariantCultureIgnoreCase))
                    return sr;
            }
            return null;
        }

        private class XboxGameSearchItemOption : GenericItemOption
        {
            public XboxSearchResultGame Game { get; set; }
            public static XboxGameSearchItemOption FromSearchResult(XboxSearchResultGame game)
            {
                return new XboxGameSearchItemOption { Name = game.Title, Description = game.Id, Game = game };
            }
        }

        private class XboxImageFileOption : ImageFileOption
        {
            public XboxImageFileOption() : base() { }
            public XboxImageFileOption(string path) : base(path) { }

            public XboxImageDetails ImageDetails { get; set; }

            public static XboxImageFileOption FromImageDetails(XboxImageDetails imageDetails)
            {
                var o = new XboxImageFileOption(imageDetails.GetResizedUrl(320, 320)) { ImageDetails = imageDetails };
                return o;
            }
        }
    }
}