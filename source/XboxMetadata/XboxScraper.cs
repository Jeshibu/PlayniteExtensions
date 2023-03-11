using PlayniteExtensions.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace XboxMetadata
{
    public interface IXboxScraper
    {
        IEnumerable<XboxSearchResultGame> Search(string query);
        XboxGameDetailsProductSummary GetGameDetails(string gameId);
        string GetStoreUrl(string gameId);
    }

    public class XboxScraper : IXboxScraper
    {
        private readonly IWebDownloader downloader;
        private readonly string market;

        public XboxScraper(IWebDownloader downloader, string market = "en-us")
        {
            this.downloader = downloader;
            this.market = market;
        }

        public IEnumerable<XboxSearchResultGame> Search(string query)
        {
            var escapedQuery = Uri.EscapeDataString(query);
            var url = $"https://www.microsoft.com/msstoreapiprod/api/autosuggest?market={market}&sources=DCatAll-Products&filter=+ClientType:StoreWeb&counts=5&query={escapedQuery}";
            var response = downloader.DownloadString(url, throwExceptionOnErrorResponse: true);
            var parsed = JsonConvert.DeserializeObject<XboxSearchResultsRoot>(response.ResponseContent);
            var output = parsed.ResultSets.SelectMany(rs => rs.Suggests).Select(SearchResultFromSuggest).Where(r => r.ProductType == "Game");
            return output;
        }

        public string GetStoreUrl(string gameId)
        {
            return $"https://www.xbox.com/{market}/games/store/-/{gameId?.ToLower()}";
        }

        public XboxGameDetailsProductSummary GetGameDetails(string gameId)
        {
            var url = GetStoreUrl(gameId);
            var response = downloader.DownloadString(url, throwExceptionOnErrorResponse: true);
            var match = Regex.Match(response.ResponseContent, @"window\.__PRELOADED_STATE__\s*=\s*(?<json>.+);\r?\n", RegexOptions.ExplicitCapture);

            if (!match.Success)
                return null;

            var jsonString = match.Groups["json"]?.Value;
            var parsed = JsonConvert.DeserializeObject<XboxGameDetailsRoot>(jsonString);
            var summary = parsed.Core2.Products.ProductSummaries[gameId];
            return summary;
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
    }
    #region details
    public class XboxGameDetailsRoot
    {
        public XboxGameDetailsCore2 Core2 { get; set; }
    }

    public class XboxGameDetailsCore2
    {
        public XboxGameDetailsProducts Products { get; set; }
    }

    public class XboxGameDetailsProducts
    {
        public Dictionary<string, XboxGameDetailsProductSummary> ProductSummaries { get; set; }
    }

    public class XboxGameDetailsProductSummary
    {
        public string ProductId { get; set; }
        public XboxGameDetailsAccessibilityCapabilities AccessibilityCapabilities { get; set; }
        public string[] AvailableOn { get; set; }
        public double? AverageRating { get; set; }
        public Dictionary<string, string> Capabilities { get; set; }
        public string[] Categories { get; set; }
        public XboxGameDetailsAgeRating Rating { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public string DeveloperName { get; set; }
        public string PublisherName { get; set; }
        public XboxGameDetailsImages Images { get; set; }
        public Dictionary<string, XboxGameLanguageSupport> LanguagesSupported { get; set; }
        public ulong MaxInstallSize { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Title { get; set; }
        //there's also a "videos" property that can't be used right now
    }

    public class XboxGameLanguageSupport
    {
        public string LanguageDisplayName { get; set; }
        public bool AreSubtitlesSupported { get; set; }
        public bool IsAudioSupported { get; set; }
        public bool IsInterfaceSupported { get; set; }
    }

    public class XboxGameDetailsImages
    {
        public XboxImageDetails BoxArt { get; set; }
        public XboxImageDetails Poster { get; set; }
        public XboxImageDetails SuperHeroArt { get; set; }
        public XboxImageDetails[] Screenshots { get; set; }
    }

    public class XboxImageDetails
    {
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public string GetResizedUrl(int maxWidth, int maxHeight, int quality = 90)
        {
            int w = Math.Min(Width, maxWidth);
            int h = Math.Min(Height, maxHeight);
            return $"{Url}?q={quality}&w={w}&h={h}";
        }
    }

    public class XboxGameDetailsAccessibilityCapabilities
    {
        public string[] Audio { get; set; }
        public string[] Gameplay { get; set; }
        public string[] Input { get; set; }
        public string[] Visual { get; set; }
        public string PublisherInformationUri { get; set; }
    }

    public class XboxGameDetailsAgeRating
    {
        public string BoardName { get; set; }
        public string Description { get; set; }
        public object[] Disclaimers { get; set; }
        public string[] Descriptors { get; set; }
        public string ImageUri { get; set; }
        public string ImageLinkUri { get; set; }
        public string[] InteractiveDescriptions { get; set; }
        public string Rating { get; set; }
        public int RatingAge { get; set; }
        public string RatingDescription { get; set; }
    }
    #endregion details

    #region search
    public class XboxSearchResultGame
    {
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string Id { get; set; }
        public string ProductType { get; set; }
    }

    public class XboxSearchResultsRoot
    {
        public string Query { get; set; }
        public XboxSearchResultSet[] ResultSets { get; set; }
    }

    public class XboxSearchResultSet
    {
        public string Source { get; set; }
        public bool FromCache { get; set; }
        public string Type { get; set; }
        public XboxSearchSuggest[] Suggests { get; set; }
    }

    public class XboxSearchSuggest
    {
        public string Source { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public bool Curated { get; set; }
        public XboxMetadataItem[] Metas { get; set; }
    }

    public class XboxMetadataItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    #endregion search

}