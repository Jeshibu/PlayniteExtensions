using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteExtensions.Tests.Common
{
    public class FakeWebDownloader : IWebDownloader
    {
        public class Redirect
        {
            public string RedirectUrl { get; set; }
            public int Depth { get; set; }

            public Redirect(string url, int depth = 0)
            {
                RedirectUrl = url;
                Depth = depth;
            }
        }

        public Dictionary<string, string> FilesByUrl { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public Dictionary<string, Redirect> RedirectsByUrl { get; } = new Dictionary<string, Redirect>(StringComparer.Ordinal);
        public List<string> CalledUrls { get; } = new List<string>();

        public CookieContainer Cookies { get; } = new CookieContainer();

        public FakeWebDownloader() { }

        public FakeWebDownloader(string url, string localFile)
        {
            FilesByUrl.Add(url, localFile);
        }

        public FakeWebDownloader(Dictionary<string, string> filesByUrl)
        {
            FilesByUrl = filesByUrl;
        }

        public void AddRedirect(string url, string redirectUrl, int depth = 1)
        {
            RedirectsByUrl.Add(url, new Redirect(redirectUrl, depth));
        }

        public virtual async Task<DownloadStringResponse> DownloadStringAsync(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<HttpRequestHeaders> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxResponseDepth = 7, CancellationToken? cancellationToken = null)
        {
            return DownloadString(url, redirectUrlGetFunc, jsCookieGetFunc, referer, headerSetter, contentType: null, throwExceptionOnErrorResponse, maxResponseDepth, cancellationToken);
        }

        public DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<HttpRequestHeaders> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7, CancellationToken? cancellationToken = null)
        {
            CalledUrls.Add(url);
            if (FilesByUrl.TryGetValue(url, out string filePath))
                return new DownloadStringResponse(url, File.ReadAllText(filePath), HttpStatusCode.OK);

            if (RedirectsByUrl.TryGetValue(url, out Redirect redir))
            {
                if (maxRedirectDepth < redir.Depth)
                    return new DownloadStringResponse(redir.RedirectUrl, null, HttpStatusCode.Redirect);
                else
                    return DownloadString(redir.RedirectUrl, redirectUrlGetFunc, jsCookieGetFunc, referer, headerSetter, contentType, throwExceptionOnErrorResponse, maxRedirectDepth, cancellationToken);
            }

            throw new Exception($"Url not accounted for: {url}");
        }
    }
}