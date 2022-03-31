using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Barnite.Scrapers
{
    public interface IWebclient
    {
        DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null);
    }

    public class DownloadStringResponse
    {
        public DownloadStringResponse(string responseUrl, string responseContent)
        {
            ResponseUrl = responseUrl;
            ResponseContent = responseContent;
        }

        public string ResponseUrl { get; set; }
        public string ResponseContent { get; set; }
    }

    public class Webclient : IWebclient
    {
        private static HttpStatusCode[] HttpRedirectStatusCodes = new[] { HttpStatusCode.Redirect, HttpStatusCode.Moved };

        public int MaxRedirectDepth { get; }
        public CookieCollection Cookies { get; private set; }

        public Webclient(int maxRedirectDepth = 7)
        {
            MaxRedirectDepth = maxRedirectDepth;
        }

        public DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null)
        {
            return DownloadString(url, redirectUrlGetFunc, jsCookieGetFunc, 0);
        }

        private DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, int depth = 0)
        {
            var uri = new Uri(url);
            var request = WebRequest.CreateHttp(uri);
            request.AllowAutoRedirect = false; //auto-redirect buries response cookies
            if (Cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(Cookies);
            }

            if (depth > 0)
                request.Referer = uri.GetLeftPart(UriPartial.Authority);

            string responseUrl;
            string responseContent;
            string redirectUrl = null;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                Cookies = Combine(Cookies, response.Cookies);
                responseUrl = response.ResponseUri.AbsoluteUri;
                responseContent = reader.ReadToEnd();
                if (HttpRedirectStatusCodes.Contains(response.StatusCode))
                {
                    redirectUrl = response.Headers[HttpResponseHeader.Location];
                    if (!string.IsNullOrWhiteSpace(redirectUrl))
                        redirectUrl = new Uri(new Uri(url), redirectUrl).AbsoluteUri;
                }
            }

            var jsCookies = jsCookieGetFunc?.Invoke(responseContent);
            Cookies = Combine(Cookies, jsCookies);

            redirectUrl = redirectUrl ?? redirectUrlGetFunc?.Invoke(url, responseContent);
            if (redirectUrl != null)
            {
                if (depth > MaxRedirectDepth)
                    return null;

                var redirectOutput = DownloadString(redirectUrl, redirectUrlGetFunc, jsCookieGetFunc, depth + 1);
                return redirectOutput;
            }
            else
            {
                return new DownloadStringResponse(responseUrl, responseContent);
            }
        }

        private CookieCollection Combine(CookieCollection a, CookieCollection b)
        {
            if (a == null || a.Count == 0) return b;
            if (b == null || b.Count == 0) return a;

            var c = new CookieCollection();
            c.Add(a);
            c.Add(b);

            return c;
        }
    }
}
