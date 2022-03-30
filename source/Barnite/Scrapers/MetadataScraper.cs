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
        string DownloadString(string url, out CookieCollection responseCookies, CookieCollection cookies = null, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null);
    }

    public class Webclient : IWebclient
    {
        private static HttpStatusCode[] HttpRedirectStatusCodes = new[] { HttpStatusCode.Redirect, HttpStatusCode.Moved };

        public int MaxRedirectDepth { get; }

        public Webclient(int maxRedirectDepth = 7)
        {
            MaxRedirectDepth = maxRedirectDepth;
        }

        public string DownloadString(string url)
        {
            using (var client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }

        public string DownloadString(string url, CookieCollection cookies = null)
        {
            return DownloadString(url, out _, cookies: cookies, null, null, 0);
        }

        public string DownloadString(string url, out CookieCollection responseCookies, CookieCollection cookies = null, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null)
        {
            return DownloadString(url, out responseCookies, cookies, redirectUrlGetFunc, jsCookieGetFunc, 0);
        }

        private string DownloadString(string url, out CookieCollection responseCookies, CookieCollection cookies = null, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, int depth = 0)
        {
            var uri = new Uri(url);
            var request = WebRequest.CreateHttp(uri);
            request.AllowAutoRedirect = false; //auto-redirect buries response cookies
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                foreach (Cookie cookie in cookies)
                {
                    request.CookieContainer.Add(cookie);
                }
            }

            if (depth > 0)
            {
                request.Referer = uri.GetLeftPart(UriPartial.Authority);
            }

            string responseContent;
            string redirectUrl = null;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                responseCookies = response.Cookies;
                responseContent = reader.ReadToEnd();
                if (HttpRedirectStatusCodes.Contains(response.StatusCode))
                {
                    redirectUrl = response.Headers[HttpResponseHeader.Location];
                    if (!string.IsNullOrWhiteSpace(redirectUrl))
                        redirectUrl = new Uri(new Uri(url), redirectUrl).AbsoluteUri;
                }
            }

            var jsCookies = jsCookieGetFunc?.Invoke(responseContent);
            if (jsCookies?.Count != 0)
            {
                responseCookies = Combine(responseCookies, jsCookies);
            }

            redirectUrl = redirectUrl ?? redirectUrlGetFunc?.Invoke(url, responseContent);
            if (redirectUrl != null)
            {
                if (depth > MaxRedirectDepth)
                    return null;

                string redirectContent = DownloadString(redirectUrl, out var redirectCookies, Combine(responseCookies, cookies), redirectUrlGetFunc, jsCookieGetFunc, depth + 1);
                responseCookies = Combine(responseCookies, redirectCookies);
                return redirectContent;
            }
            else
            {
                return responseContent;
            }
        }

        private CookieCollection Combine(CookieCollection a, CookieCollection b)
        {
            if (a == null) return b;
            if (b == null) return a;

            var c = new CookieCollection();
            c.Add(a);
            c.Add(b);

            return c;
        }
    }

    public abstract class MetadataScraper
    {
        public abstract string Name { get; }

        protected IPlatformUtility PlatformUtility { get; set; }
        protected IWebclient Webclient { get; set; }

        private CookieCollection Cookies { get; set; }

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
            var html = Webclient.DownloadString(searchUrl, out var responseCookies, Cookies, ScrapeRedirectUrl, ScrapeJsCookies);

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

        protected virtual CookieCollection ScrapeJsCookies(string html)
        {
            return null;
        }

        protected virtual string ScrapeRedirectUrl(string requestUrl, string html)
        {
            return null;
        }
    }

    public class GameLink
    {
        public string Name;
        public string Url;
    }
}
