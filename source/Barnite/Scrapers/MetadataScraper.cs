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
        protected IWebclient Webclient { get; set; }


        public MetadataScraper(IPlatformUtility platformUtility, IWebclient webclient)
        {
            PlatformUtility = platformUtility;
            Webclient = webclient;
        }

        protected abstract string GetSearchUrlFromBarcode(string barcode);

        protected string GetAbsoluteUrl(string relativeUrl)
        {
            if (relativeUrl == null)
                return null;

            var baseUri = new Uri(GetSearchUrlFromBarcode("1"));
            var absoluteUri = new Uri(baseUri, relativeUrl);
            return absoluteUri.AbsoluteUri;
        }

        public GameMetadata GetMetadataFromBarcode(string barcode)
        {
            var searchUrl = GetSearchUrlFromBarcode(barcode);
            var response = Webclient.DownloadString(searchUrl, ScrapeRedirectUrl, ScrapeJsCookies);

            var data = ScrapeGameDetailsHtml(response.ResponseContent);
            if (data != null)
            {
                SetLink(response, data);
                return data;
            }

            //so that wasn't a game details page; try and parse it as a search result page instead
            var links = ScrapeSearchResultHtml(response.ResponseContent).ToList();
            if (links != null && links.Count == 1)
            {
                response = Webclient.DownloadString(links[0].Url, ScrapeRedirectUrl, ScrapeJsCookies);
                data = ScrapeGameDetailsHtml(response.ResponseContent);
                SetLink(response, data);
                return data;
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

        protected virtual CookieCollection ScrapeJsCookies(string html)
        {
            return null;
        }

        protected virtual string ScrapeRedirectUrl(string requestUrl, string html)
        {
            return null;
        }

        private void SetLink(DownloadStringResponse response, GameMetadata data)
        {
            if (data == null || response == null)
                return;

            var links = data.Links ?? (data.Links = new List<Link>());
            links.Add(new Link(this.Name, response.ResponseUrl));
        }
    }

    public class GameLink
    {
        public string Name;
        public string Url;
    }
}
