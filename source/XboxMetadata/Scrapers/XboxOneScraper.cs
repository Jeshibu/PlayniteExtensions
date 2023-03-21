using Newtonsoft.Json;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XboxMetadata.Scrapers
{
    public class XboxOneScraper : BaseXboxScraper
    {
        public XboxOneScraper(IWebDownloader downloader, IPlatformUtility platformUtility) : base(downloader, platformUtility)
        {
        }

        public override string Key { get; } = "XboxOne";
        public override int ExecutionOrder { get; } = 1;

        public override async Task<XboxGameDetails> GetDetailsAsync(XboxMetadataSettings settings, string id)
        {
            var url = $"https://www.xbox.com/{settings.Market}/games/store/-/{id?.ToLower()}";
            var response = await downloader.DownloadStringAsync(url, throwExceptionOnErrorResponse: true);
            var match = Regex.Match(response.ResponseContent, @"window\.__PRELOADED_STATE__\s*=\s*(?<json>.+);\r?\n", RegexOptions.ExplicitCapture);

            if (!match.Success)
                return null;

            var jsonString = match.Groups["json"]?.Value;
            var parsed = JsonConvert.DeserializeObject<XboxGameDetailsRoot>(jsonString);
            var summary = parsed.Core2.Products.ProductSummaries[id];

            platformUtility.GetPlatformsFromName(summary.Title, out string trimmedTitle);
            var features = new List<string>();

            if (settings.ImportAccessibilityFeatures && summary.AccessibilityCapabilities != null)
            {
                features.AddRange(summary.AccessibilityCapabilities?.Audio.Select(c => "Accessibility: Audio: " + c));
                features.AddRange(summary.AccessibilityCapabilities?.Gameplay.Select(c => "Accessibility: Gameplay: " + c));
                features.AddRange(summary.AccessibilityCapabilities?.Input.Select(c => "Accessibility: Input: " + c));
                features.AddRange(summary.AccessibilityCapabilities?.Visual.Select(c => "Accessibility: Visual: " + c));
            }

            Regex multiSpace = new Regex(@"\s{2,}");

            if (summary.Capabilities != null)
                features.AddRange(summary.Capabilities.Values.Select(c => multiSpace.Replace(c, " ")));

            features.Sort();
            var links = new List<Link> { new Link("Xbox Store", url) };
            if (settings.ImportAccessibilityFeatures && summary.AccessibilityCapabilities?.PublisherInformationUri != null)
                links.Add(new Link("Accessibility information", summary.AccessibilityCapabilities.PublisherInformationUri));

            string ageRating = null;
            if (summary.ContentRating != null)
                ageRating = summary.ContentRating.BoardName + " " + ShortenRatingString(summary.ContentRating.Rating);

            var output = new XboxGameDetails
            {
                ScraperKey = Key,
                Id = id,
                Title = trimmedTitle,
                Description = Regex.Replace(summary.Description, "\r?\n", "<br>$0"),
                Platforms = summary.AvailableOn?.SelectMany(p => platformUtility.GetPlatforms(p)).ToList(),
                Developers = GetCompanies(summary.DeveloperName).ToList(),
                Publishers = GetCompanies(summary.PublisherName).ToList(),
                CommunityScore = (int)(summary.AverageRating * 20),
                InstallSize = summary.MaxInstallSize,
                Genres = summary.Categories?.ToList(),
                Features = features,
                ReleaseDate = summary.ReleaseDate,
                Links = links,
                AgeRating = ageRating,
                Covers = GetImages(summary, settings.Cover),
                Backgrounds = GetImages(summary, settings.Background),
            };

            return output;
        }

        public override async Task<List<XboxGameSearchResultItem>> SearchAsync(XboxMetadataSettings settings, string query)
        {
            var escapedQuery = Uri.EscapeDataString(query);
            //var url = $"https://www.microsoft.com/msstoreapiprod/api/autosuggest?market={market}&sources=DCatAll-Products&filter=+ClientType:StoreWeb&counts=5&query={escapedQuery}";
            var url = $"https://www.microsoft.com/msstoreapiprod/api/autosuggest?market={settings.Market}&clientId={Guid.Empty}&sources=Microsoft-Terms,Iris-Products,DCatAll-Products&filter=+ClientType:StoreWeb&counts=5,1,5&query={escapedQuery}";
            var response = await downloader.DownloadStringAsync(url, throwExceptionOnErrorResponse: true);
            var parsed = JsonConvert.DeserializeObject<XboxSearchResultsRoot>(response.ResponseContent);
            var searchResults = parsed.ResultSets.FirstOrDefault(rs => rs.Source == "dcatall-products")?.Suggests.Select(SearchResultFromSuggest).Where(r => r.ProductType == "Game");
            var output = new List<XboxGameSearchResultItem>();
            if (searchResults == null)
                return output;

            foreach (var sr in searchResults)
            {
                var platforms = platformUtility.GetPlatformsFromName(sr.Title, out string trimmedTitle);
                output.Add(new XboxGameSearchResultItem { ScraperKey = Key, Id = sr.Id, Title = trimmedTitle, Platforms = platforms.ToList() });
            }
            return output;
        }

        private static XboxSearchResultGame SearchResultFromSuggest(XboxSearchSuggest suggest)
        {
            var imgUrl = new Uri(new Uri("https://www.xbox.com/"), suggest.ImageUrl).AbsoluteUri;

            var id = suggest.Metas.FirstOrDefault(m => m.Key == "BigCatalogId")?.Value;
            var productType = suggest.Metas.FirstOrDefault(m => m.Key == "ProductType")?.Value;

            return new XboxSearchResultGame
            {
                Title = suggest.Title,
                ImageUrl = imgUrl,
                Id = id,
                ProductType = productType,
            };
        }

        private static IEnumerable<string> GetCompanies(string name)
        {
            var names = name?.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return names.NullIfEmpty()?.Select(n => n.Trim().TrimCompanyForms());
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

        private List<ImageData> GetImages(XboxGameDetailsProductSummary summary, XboxImageSourceSettings imgSettings)
        {
            if (summary.Images == null)
                return new List<ImageData>(); ;

            var potentialImages = new List<XboxImageDetails>();
            foreach (var fieldSetting in imgSettings.Fields)
            {
                if (!fieldSetting.Checked)
                    continue;

                switch (fieldSetting.Field)
                {
                    case ImageSourceField.BoxArt:
                        potentialImages.Add(summary.Images.BoxArt);
                        break;
                    case ImageSourceField.Poster:
                        potentialImages.Add(summary.Images.Poster);
                        break;
                    case ImageSourceField.SuperHeroArt:
                        potentialImages.Add(summary.Images.SuperHeroArt);
                        break;
                    case ImageSourceField.Screenshots:
                        potentialImages.AddRange(summary.Images.Screenshots);
                        break;
                    default:
                        continue;
                }
            }

            Predicate<XboxImageDetails> FilterImageBySize = i =>
            {
                bool smallerThanMinimum = i == null || (i.Width < imgSettings.MinWidth && i.Height < imgSettings.MinHeight);
                if (smallerThanMinimum)
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

            return potentialImages.FindAll(FilterImageBySize)
                                  .ToDictionarySafe(i => i.Url).Values //deduplicate by Url - for old games BoxArt and Poster are the same
                                  .Select(i => ToImageData(i, imgSettings))
                                  .ToList();
        }

        private static ImageData ToImageData(XboxImageDetails imgDetails, XboxImageSourceSettings imgSettings)
        {
            string url;
            if (imgDetails.Height > imgSettings.MaxHeight || imgDetails.Width > imgSettings.MaxWidth)
                url = imgDetails.GetResizedUrl(imgSettings.MaxWidth, imgSettings.MaxHeight, 100);
            else
                url = imgDetails.Url;

            return new ImageData { Url = url, Width = imgDetails.Width, Height = imgDetails.Height, ThumbnailUrl = imgDetails.GetResizedUrl(320, 180, quality: 90) };
        }
    }
}
