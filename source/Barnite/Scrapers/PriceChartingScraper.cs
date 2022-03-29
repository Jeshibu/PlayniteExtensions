using HtmlAgilityPack;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Barnite.Scrapers
{
    public class PriceChartingScraper : MetadataScraper
    {
        public override string Name { get; } = "PriceCharting";

        public PriceChartingScraper(IPlatformUtility platformUtility, IWebclient webclient)
            : base(platformUtility, webclient)
        {
        }

        protected override string GetSearchUrlFromBarcode(string barcode)
        {
            return "https://www.pricecharting.com/search-products?category=videogames&q=" + HttpUtility.UrlEncode(barcode);
        }

        protected override GameMetadata ScrapeGameDetailsHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var titleNode = doc.DocumentNode.SelectSingleNode("//div[@id='product_details']/h1") ?? doc.DocumentNode.SelectSingleNode("//h1[@id='product_name']");
            if (titleNode == null)
                return null;

            string title = HtmlDecodeAndNormalizeWhitespace(titleNode.SelectSingleNode("text()")?.InnerText);
            title = title?.Trim(' ', '|');
            string platform = HtmlDecodeAndNormalizeWhitespace(titleNode.SelectSingleNode("a")?.InnerText);
            if (title == null || platform == null)
                return null;

            var data = new GameMetadata
            {
                Name = title,
                Platforms = new HashSet<MetadataProperty> { PlatformUtility.GetPlatform(platform) },
            };

            string coverUrl = doc.DocumentNode.SelectSingleNode("//div[@class='cover']/img[@src]")?.Attributes["src"].Value;
            if (!string.IsNullOrEmpty(coverUrl))
                data.CoverImage = new MetadataFile(coverUrl);

            return data;
        }

        protected override IEnumerable<GameLink> ScrapeSearchResultHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var links = doc.DocumentNode.SelectNodes("//h2[@class='product_name']/a[@href]");

            if (links == null || links.Count == 0)
                links = doc.DocumentNode.SelectNodes("//table[@id='games_table']/tbody/tr/td[@class='title']/a[@href]");

            if (links == null || links.Count == 0)
                yield break;

            var baseUri = new Uri("https://www.pricecharting.com");

            foreach (var a in links)
            {
                yield return new GameLink { Name = HtmlDecodeAndNormalizeWhitespace(a.InnerText), Url = GetAbsoluteUrl(a.Attributes["href"].Value) };
            }
        }
    }
}
