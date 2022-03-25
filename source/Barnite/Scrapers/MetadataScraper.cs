using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace Barnite.Scrapers
{
    public abstract class MetadataScraper
    {
        public abstract string Name { get; }

        protected IPlatformUtility PlatformUtility { get; set; }

        protected Func<string, string> DownloadString { get; set; }

        public MetadataScraper(IPlatformUtility platformUtility, Func<string, string> downloadString = null)
        {
            PlatformUtility = platformUtility;
            DownloadString = downloadString ?? DownloadStringDefault;
        }

        private static string DownloadStringDefault(string url)
        {
            using (var client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }

        protected abstract string GetSearchUrlFromBarcode(string barcode);

        public GameMetadata GetMetadataFromBarcode(string barcode)
        {
            var searchUrl = GetSearchUrlFromBarcode(barcode);
            var html = DownloadString(searchUrl);

            var data = ScrapeGameDetailsHtml(html);
            if (data != null)
                return data;

            //so that wasn't a game details page; try and parse it as a search result page instead
            var links = ScrapeSearchResultHtml(html).ToList();
            if (links != null && links.Count == 1)
            {
                html = DownloadString(links[0].Url);
                return ScrapeGameDetailsHtml(html);
            }

            return null;
        }

        protected static string HtmlDecodeAndNormalizeWhitespace(string input)
        {
            if (input == null)
                return null;

            return Regex.Replace(HttpUtility.HtmlDecode(input), @"\s", " ").Trim();
        }

        /// <summary>
        /// Scrape an HTML page for game metadata. Implementing classes should fail fast (and return null) out of this method if this page does not represent game metadata.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected abstract GameMetadata ScrapeGameDetailsHtml(string html);

        /// <summary>
        /// Scrape a list of links to game detail pages from a search result.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected abstract IEnumerable<GameLink> ScrapeSearchResultHtml(string html);
    }

    public class GameLink
    {
        public string Name;
        public string Url;
    }
}
