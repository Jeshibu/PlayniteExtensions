using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XboxMetadata.Scrapers;

public class XboxOneScraper(IWebDownloader downloader, IPlatformUtility platformUtility) : BaseXboxScraper(downloader, platformUtility)
{
    public override string Key { get; } = "XboxOne";
    public override int ExecutionOrder { get; } = 1;
    private readonly ILogger logger = LogManager.GetLogger();

    public override async Task<XboxGameDetails> GetDetailsAsync(XboxMetadataSettings settings, string id, string url)
    {
        //var url = $"https://www.xbox.com/{settings.Market}/games/store/-/{id?.ToLower()}";
        var response = await downloader.DownloadStringAsync(url, throwExceptionOnErrorResponse: true);
        var responseUrl = new Uri(response.ResponseUrl);

        switch (responseUrl.Host)
        {
            case "www.xbox.com": return GetXboxDotComGameDetails(settings, id, response);
            case "www.microsoft.com": return await GetMicrosoftDotComGameDetailsAsync(settings, id, response);
            default:
                logger.Warn($"Unknown domain: {response.ResponseUrl}, status: {response.StatusCode}");
                return null;
        }
    }

    public async Task<XboxGameDetails> GetMicrosoftDotComGameDetailsAsync(XboxMetadataSettings settings, string id, DownloadStringResponse response)
    {
        //turns out the Detail-[number]-toggle-target IDs vary, so can't be used

        var parser = new HtmlParser();
        var doc = await parser.ParseAsync(response.ResponseContent);

        var output = new XboxGameDetails() { Url = response.ResponseUrl };

        output.Title = SelectSingleNodeContent(doc, "#DynamicHeading_productTitle");
        output.Platforms = SelectNodesTextContent(doc, "div#module-available-on > div > a.c-tag").SelectMany(platformUtility.GetPlatforms).ToList();
        output.Features = SelectNodesTextContent(doc, "div#module-capabilities > div > a.c-tag");
        output.Description = SelectSingleNodeContent(doc, "p#product-description");
        var screenshotsJson = doc.QuerySelector("div.cli_gallery_json[data-slides-json]")?.GetAttribute("data-slides-json");
        output.Backgrounds = GetScreenshots(screenshotsJson).ToList();
        //output.Developers = SelectNodesTextContent(doc, "div#Detail-57779-toggle-target > span");
        //output.Publishers = SelectNodesTextContent(doc, "div#Detail-47474-toggle-target > span > span");
        output.Genres = SelectNodesTextContent(doc, "div#category-toggle-target a");
        var coverUrl = doc.QuerySelector("div.pi-product-image img")?.GetAttribute("src");
        if (!string.IsNullOrWhiteSpace(coverUrl) && settings.Cover.Fields.Any(f => f.Checked && f.Field == ImageSourceField.AppStoreProductImage))
            output.Covers.Add(UrlToImageData(coverUrl));

        if (DoesCultureExist(settings.Market))
        {
            var culture = new CultureInfo(settings.Market);
            var releaseDateString = SelectSingleNodeContent(doc, "div#releaseDate-toggle-target > span");
            if (!string.IsNullOrWhiteSpace(releaseDateString) && DateTime.TryParse(releaseDateString, culture, DateTimeStyles.AssumeLocal, out var releaseDate))
                output.ReleaseDate = releaseDate;

            //output.InstallSize = SelectSingleNodeContent(doc, "div#Detail-57780-toggle-target > span")?.ParseInstallSize(culture);
        }
        else
        {
            logger.Warn($"Could not find culture {settings.Market}");
        }

        return output;
    }

    private static bool DoesCultureExist(string cultureName)
    {
        return CultureInfo.GetCultures(CultureTypes.AllCultures).Any(culture => string.Equals(culture.Name, cultureName, StringComparison.CurrentCultureIgnoreCase));
    }

    private static string SelectSingleNodeContent(IHtmlDocument doc, string selector) => doc.QuerySelector(selector)?.TextContent.HtmlDecode();

    private static List<string> SelectNodesTextContent(IHtmlDocument doc, string selector) => doc.QuerySelectorAll(selector)?.Select(x => x.TextContent.HtmlDecode()).ToList();

    private static IEnumerable<ImageData> GetScreenshots(string screenshotsJson)
    {
        if (string.IsNullOrWhiteSpace(screenshotsJson)) yield break;

        var screenshots = JsonConvert.DeserializeObject<GalleryImage[]>(screenshotsJson);
        if (screenshots == null)
            yield break;

        foreach (var s in screenshots)
        {
            var ifbp = s.ImageForBreakPoints.Any() ? s.ImageForBreakPoints.Aggregate((a, b) => a.ForMinWidth < b.ForMinWidth ? a : b) : null;

            yield return UrlToImageData(s.DefaultGalleryImageUrl, ifbp?.Uri);
        }
    }

    private static ImageData UrlToImageData(string url, string thumbnailUrl = null)
    {
        var output = new ImageData { Url = GetAbsoluteUrl(url) };

        if (thumbnailUrl != null)
            output.ThumbnailUrl = GetAbsoluteUrl(thumbnailUrl);

        if (GetImageDimensionsFromQueryString(url, out int width, out int height))
        {
            output.Width = width;
            output.Height = height;
        }
        return output;
    }

    private static bool GetImageDimensionsFromQueryString(string imageUrl, out int width, out int height)
    {
        bool success = false;
        width = 0;
        height = 0;
        var matches = Regex.Matches(imageUrl, @"[&?]([a-z])=([0-9]+)");
        foreach (Match match in matches)
        {
            var queryStringParameter = match.Groups[1].Value;
            var dimension = int.Parse(match.Groups[2].Value);
            switch (queryStringParameter)
            {
                case "w":
                    success = true;
                    width = dimension;
                    break;
                case "h":
                    success = true;
                    height = dimension;
                    break;
            }
        }
        return success;
    }

    private static string GetAbsoluteUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        return new Uri(new Uri("https://www.microsoft.com"), url).ToString();
    }

    private class GalleryImage
    {
        public int DeviceType { get; set; }
        public int Index { get; set; }
        public string DefaultImageUrl { get; set; }
        public string DefaultGalleryImageUrl { get; set; }
        public string AltText { get; set; }
        public ImageForBreakPoint[] ImageForBreakPoints { get; set; } = new ImageForBreakPoint[0];
    }

    private class ImageForBreakPoint
    {
        public int ForMinWidth { get; set; }
        public string Uri { get; set; }
    }

    private XboxGameDetails GetXboxDotComGameDetails(XboxMetadataSettings settings, string id, DownloadStringResponse response)
    {
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

        Regex multiSpace = new(@"\s{2,}");

        if (summary.Capabilities != null)
            features.AddRange(summary.Capabilities.Values.Select(c => multiSpace.Replace(c, " ")));

        features.Sort();
        var links = new List<Link> { new("Xbox Store", response.ResponseUrl) };
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
            Url = response.ResponseUrl,
        };

        return output;
    }

    public static string GetSearchUrl(string market, string query)
    {
        // As of 2023-07-22, adding a colon or tilde to any query makes it return no results (unescaped search index query character maybe?)
        var escapedQuery = Uri.EscapeDataString(Regex.Replace(query, @"[^\p{L}\p{M}\p{N}\p{Zs}]", string.Empty));
        return $"https://www.microsoft.com/msstoreapiprod/api/autosuggest?market={market}&sources=DCatAll-Products,xSearch-Products&filter=+ClientType:StoreWeb&counts=20,20&query={escapedQuery}";
    }

    public override async Task<List<XboxGameSearchResultItem>> SearchAsync(XboxMetadataSettings settings, string query)
    {
        var url = GetSearchUrl(settings.Market, query);
        var response = await downloader.DownloadStringAsync(url, throwExceptionOnErrorResponse: true);
        var parsed = JsonConvert.DeserializeObject<XboxSearchResultsRoot>(response.ResponseContent);
        var searchResults = parsed.ResultSets.Where(rs => rs.Type == "product")
            ?.SelectMany(rs => rs.Suggests)
            .Where(sug => sug.Source == "Game")
            .Select(SearchResultFromSuggest)
            .ToDictionarySafe(sr => sr.Id).Values; //deduplicate by ID - DCatAll-Products and xSearch-Products can overlap
        var output = new List<XboxGameSearchResultItem>();
        if (searchResults == null)
            return output;

        foreach (var sr in searchResults)
        {
            var platforms = platformUtility.GetPlatformsFromName(sr.Title, out string trimmedTitle);
            output.Add(new XboxGameSearchResultItem
            {
                ScraperKey = Key,
                Id = sr.Id,
                Url = sr.Url,
                Title = trimmedTitle,
                Platforms = platforms.ToList()
            });
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
            Url = suggest.Url,
        };
    }

    private static IEnumerable<string> GetCompanies(string name)
    {
        var names = name?.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        return names.NullIfEmpty()?.Select(n => n.Trim().TrimCompanyForms());
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

    private List<ImageData> GetImages(XboxGameDetailsProductSummary summary, XboxImageSourceSettings imgSettings)
    {
        if (summary.Images == null)
            return [];

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

        bool FilterImageBySize(XboxImageDetails i)
        {
            bool smallerThanMinimum = i == null || (i.Width < imgSettings.MinWidth && i.Height < imgSettings.MinHeight);
            if (smallerThanMinimum)
                return false;

            return imgSettings.AspectRatio switch
            {
                AspectRatio.Vertical => i.Width < i.Height,
                AspectRatio.Horizontal => i.Width > i.Height,
                AspectRatio.Square => i.Width == i.Height,
                _ => true,
            };
        }

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

    public override string FixUrl(string url) => new Uri(new Uri("https://www.xbox.com/en-US/"), url).ToString();
}
