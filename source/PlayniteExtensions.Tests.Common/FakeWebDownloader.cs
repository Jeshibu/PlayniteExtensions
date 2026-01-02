using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteExtensions.Tests.Common;

public class FakeWebDownloader : IWebDownloader
{
    public class Redirect(string url, int depth = 0)
    {
        public string RedirectUrl { get; set; } = url;
        public int Depth { get; set; } = depth;
    }

    public Dictionary<string, string> FilesByUrl { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, Redirect> RedirectsByUrl { get; } = new(StringComparer.Ordinal);
    public List<string> CalledUrls { get; } = [];

    public CookieContainer Cookies { get; } = new();
    public string UserAgent { get; set; }

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
        RedirectsByUrl.Add(url, new(redirectUrl, depth));
    }

    public virtual Task<DownloadStringResponse> DownloadStringAsync(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<HttpRequestHeaders> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxResponseDepth = 7, CancellationToken cancellationToken = default, bool getContent = true)
    {
        return Task.FromResult(DownloadString(url, redirectUrlGetFunc, jsCookieGetFunc, referer, headerSetter, contentType: null, throwExceptionOnErrorResponse, maxResponseDepth, cancellationToken, getContent));
    }

    public DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<HttpRequestHeaders> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7, CancellationToken cancellationToken = default, bool getContent = true)
    {
        CalledUrls.Add(url);
        if (FilesByUrl.TryGetValue(url, out string filePath))
            return new(url, File.ReadAllText(filePath), HttpStatusCode.OK);

        if (RedirectsByUrl.TryGetValue(url, out Redirect redir))
        {
            if (maxRedirectDepth < redir.Depth)
                return new(redir.RedirectUrl, null, HttpStatusCode.Redirect);
            else
                return DownloadString(redir.RedirectUrl, redirectUrlGetFunc, jsCookieGetFunc, referer, headerSetter, contentType, throwExceptionOnErrorResponse, maxRedirectDepth, cancellationToken);
        }

        throw new($"Url not accounted for: {url}");
    }

    public Task<DownloadStringResponse> PostAsync(string url, string body, Action<HttpRequestHeaders> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, CancellationToken cancellationToken = default, bool getContent = true)
    {
        CalledUrls.Add(url);
        if (FilesByUrl.TryGetValue(url, out string filePath))
            return Task.FromResult(new DownloadStringResponse(url, File.ReadAllText(filePath), HttpStatusCode.OK));

        throw new($"Url not accounted for: {url}");
    }
}
