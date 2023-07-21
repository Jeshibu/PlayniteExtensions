using AngleSharp.Parser.Html;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace XboxMetadata.Scrapers
{
    public class Xbox360Scraper : BaseXboxScraper
    {
        public Xbox360Scraper(IWebDownloader downloader, IPlatformUtility platformUtility) : base(downloader, platformUtility)
        {
        }

        public override string Key { get; } = "Xbox360";

        public override int ExecutionOrder { get; } = 10;

        public override async Task<XboxGameDetails> GetDetailsAsync(XboxMetadataSettings settings, string id, string url)
        {
            var culture = new CultureInfo(settings.Market);
            //var url = $"https://marketplace.xbox.com/{settings.Market}/Product/-/{id}";
            var response = await downloader.DownloadStringAsync(url, throwExceptionOnErrorResponse: true);

            HtmlParser parser = new HtmlParser();
            var doc = await parser.ParseAsync(response.ResponseContent);

            var output = new XboxGameDetails() { Url = url };
            output.Title = doc.QuerySelector("div#gameDetails > h1")?.TextContent;
            var boxartUrl = doc.QuerySelector("img.boxart")?.GetAttribute("src");
            if (boxartUrl != null && settings.Cover.MinWidth <= 219 && settings.Cover.MinHeight <= 300)
                output.Covers.Add(new ImageData { Url = boxartUrl, Width = 219, Height = 300 });

            if (settings.Background.MinWidth <= 1000 && settings.Background.MinHeight <= 562)
            {
                var images = doc.QuerySelectorAll("div#MediaControl > div.image > img");
                if (images != null)
                {
                    foreach (var el in images)
                    {
                        output.Backgrounds.Add(new ImageData { Url = el.GetAttribute("src"), Width = 1000, Height = 562 });
                    }
                }
            }

            var fileSizeElement = doc.QuerySelector($"div#p{id} > div > ul > li.FileSize");
            if (fileSizeElement != null)
            {
                output.Platforms.Add(new MetadataSpecProperty("xbox360"));
                var filesizeString = fileSizeElement.ChildNodes.FirstOrDefault(n => n.NodeType == AngleSharp.Dom.NodeType.Text)?.TextContent;
                output.InstallSize = filesizeString.ParseInstallSize(culture);
            }

            var productPublishing = doc.QuerySelectorAll("ul#ProductPublishing > li")?.Select(x => GetFirstTextNodeContent(x)).Where(x => x != null);
            foreach (var item in productPublishing)
            {
                if (DateTime.TryParse(item, culture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out DateTime releaseDate))
                    output.ReleaseDate = releaseDate;
            }

            var features = doc.QuerySelectorAll("div.capabilities > ul > li")?.Select(x => x.TextContent);
            if (features != null)
                output.Features.AddRange(features);

            var communityRatingText = doc.QuerySelector("div.UserRatingStarStrip > span.screen-reader-text")?.TextContent;
            if (communityRatingText != null)
            {
                communityRatingText = new string(communityRatingText.TakeWhile(c => !char.IsWhiteSpace(c)).ToArray());
                if (double.TryParse(communityRatingText, NumberStyles.Float, culture, out double communityRating) && communityRating != default)
                    output.CommunityScore = (int)(communityRating * 20);
            }

            return output;
        }

        public override async Task<List<XboxGameSearchResultItem>> SearchAsync(XboxMetadataSettings settings, string query)
        {
            var escapedQuery = Uri.EscapeDataString(query);
            var url = $"https://marketplace.xbox.com/{settings.Market}/Search?query={escapedQuery}&DownloadType=Game";
            var response = await downloader.DownloadStringAsync(url, throwExceptionOnErrorResponse: true);

            HtmlParser parser = new HtmlParser();
            var doc = await parser.ParseAsync(response.ResponseContent);

            var output = new List<XboxGameSearchResultItem>();

            var searchResultElements = doc.QuerySelectorAll(".ProductResults > div > div");
            if (searchResultElements == null)
                return output;

            var culture = new CultureInfo(settings.Market);

            foreach (var searchResultElement in searchResultElements)
            {
                var id = searchResultElement.Attributes["id"].Value.Substring(1); //remove the p from the start of the ID attribute
                var titleLink = searchResultElement.QuerySelector("div > div.Game > a");
                var detailsUrl = titleLink.GetAttribute("href");
                var item = new XboxGameSearchResultItem
                {
                    ScraperKey = Key,
                    Id = id,
                    Url = detailsUrl,
                    Title = titleLink.TextContent,
                    Platforms = new List<MetadataProperty> { new MetadataSpecProperty("xbox360") }
                };

                output.Add(item);
                var productMetas = searchResultElement.QuerySelectorAll("ul.ProductMeta > li");
                if (productMetas == null)
                    continue;

                foreach (var pm in productMetas)
                {
                    var val = GetFirstTextNodeContent(pm);

                    if (val != null && DateTime.TryParse(val, culture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out DateTime releaseDate))
                        item.ReleaseDate = releaseDate;
                }
            }
            return output;
        }

        private static string GetFirstTextNodeContent(AngleSharp.Dom.INode node)
        {
            return node.ChildNodes.FirstOrDefault(n => n.NodeType == AngleSharp.Dom.NodeType.Text && !string.IsNullOrEmpty(n.TextContent))?.TextContent;
        }
    }
}
