using HtmlAgilityPack;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Web;
using System.Linq;

namespace Barnite.Scrapers
{
    public abstract class DuckDuckGoScraper : MetadataScraper
    {
        protected abstract string SearchDomain { get; }
        protected abstract bool IsGameUrl(string url);

        protected override string GetSearchUrlFromBarcode(string barcode)
        {
            return $"https://html.duckduckgo.com/html/?q={HttpUtility.UrlEncode(barcode)}+site%3A{SearchDomain}";
        }

        protected override IEnumerable<GameLink> ScrapeSearchResultHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var itemNodes = doc.DocumentNode.SelectNodes($"//a[@href][@class='result__a']");
            if (itemNodes == null)
                yield break;

            foreach (var itemNode in itemNodes)
            {
                var name = itemNode.InnerText.HtmlDecode();
                var url = itemNode.Attributes["href"].Value;
                url = GetUrlFromDuckDuckGoResultUrl(url);
                if (IsGameUrl(url))
                    yield return new GameLink { Name = name, Url = url };
            }
        }

        private static string GetUrlFromDuckDuckGoResultUrl(string url)
        {
            var uri = new Uri(baseUri: new Uri("https://html.duckduckgo.com"), relativeUri: url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            return query.Get("uddg") ?? url;
        }
    }
}
