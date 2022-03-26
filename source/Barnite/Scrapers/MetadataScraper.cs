using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace Barnite.Scrapers
{
    public interface IWebclient
    {
        string DownloadString(string url, CookieCollection cookies = null);
        string DownloadString(string url, out CookieCollection responseCookies, CookieCollection cookies = null);
    }

    public class Webclient : IWebclient
    {
        public string DownloadString(string url)
        {
            using (var client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }

        public string DownloadString(string url, CookieCollection cookies = null)
        {
            return DownloadString(url, out _, cookies);
        }

        public string DownloadString(string url, out CookieCollection responseCookies, CookieCollection cookies = null)
        {
            var uri = new Uri(url);
            var request = WebRequest.CreateHttp(uri);
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                foreach (Cookie cookie in cookies)
                {
                    request.CookieContainer.Add(cookie);
                }
            }
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                responseCookies = response.Cookies;
                return reader.ReadToEnd();
            }
        }
    }

    public abstract class MetadataScraper
    {
        public abstract string Name { get; }

        protected IPlatformUtility PlatformUtility { get; set; }
        protected IWebclient Webclient { get; set; }
        public bool BlocksRequestsWithoutCookies { get; protected set; }

        private CookieCollection Cookies { get; set; }

        public MetadataScraper(IPlatformUtility platformUtility, IWebclient webclient, bool blocksRequestsWithoutCookies = false)
        {
            PlatformUtility = platformUtility;
            Webclient = webclient;
            BlocksRequestsWithoutCookies = blocksRequestsWithoutCookies;
        }

        protected abstract string GetSearchUrlFromBarcode(string barcode);

        public GameMetadata GetMetadataFromBarcode(string barcode)
        {
            var searchUrl = GetSearchUrlFromBarcode(barcode);
            var html = Webclient.DownloadString(searchUrl, out var responseCookies, Cookies);

            if (BlocksRequestsWithoutCookies && (Cookies = ScrapeCookieBlockingPageHtml(html, responseCookies))?.Count > 0)
            {
                html = Webclient.DownloadString(searchUrl, Cookies);
            }

            var data = ScrapeGameDetailsHtml(html);
            if (data != null)
                return data;

            //so that wasn't a game details page; try and parse it as a search result page instead
            var links = ScrapeSearchResultHtml(html).ToList();
            if (links != null && links.Count == 1)
            {
                html = Webclient.DownloadString(links[0].Url, Cookies);
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

        protected virtual CookieCollection ScrapeCookieBlockingPageHtml(string html, CookieCollection responseCookies)
        {
            var cookies = responseCookies ?? new CookieCollection();
            return cookies;
        }
    }

    public class GameLink
    {
        public string Name;
        public string Url;
    }
}
