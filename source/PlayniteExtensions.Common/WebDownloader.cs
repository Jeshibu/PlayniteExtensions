using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteExtensions.Common;

public delegate void DownloadProgressCallback(long downloadedBytes, long totalBytes);
public interface IWebDownloader
{
    /// <summary>
    /// The total collection of cookies used both as input for requests and output for responses
    /// </summary>
    CookieContainer Cookies { get; }
    DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<HttpRequestHeaders> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7, CancellationToken? cancellationToken = null, bool getContent = true);
    Task<DownloadStringResponse> DownloadStringAsync(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<HttpRequestHeaders> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7, CancellationToken? cancellationToken = null, bool getContent = true);
}

public class DownloadStringResponse(string responseUrl, string responseContent, HttpStatusCode statusCode)
{
    public string ResponseUrl { get; set; } = responseUrl;
    public string ResponseContent { get; set; } = responseContent;
    public HttpStatusCode StatusCode { get; set; } = statusCode;
}

public class WebDownloader : IWebDownloader
{
    private DangerouslySimpleCookieContainer cookieContainer;
    private HttpClient httpClient;
    private ILogger logger = LogManager.GetLogger();
    public static HttpStatusCode[] HttpRedirectStatusCodes = [HttpStatusCode.Redirect, HttpStatusCode.Moved, HttpStatusCode.TemporaryRedirect, (HttpStatusCode)308];

    public CookieContainer Cookies => cookieContainer.Container;
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0";
    public string Accept { get; set; } = "text/html,application/json,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";

    public WebDownloader()
    {
        cookieContainer = new DangerouslySimpleCookieContainer(new HttpClientHandler());
        httpClient = new HttpClient(cookieContainer, false);
    }

    public DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<HttpRequestHeaders> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7, CancellationToken? cancellationToken = null, bool getContent = true)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var output = DownloadStringAsync(url, redirectUrlGetFunc, jsCookieGetFunc, referer, headerSetter, contentType, throwExceptionOnErrorResponse, maxRedirectDepth, 0, cancellationToken, getContent).Result;
        sw.Stop();
        logger.Info($"Call to {url} completed in {sw.Elapsed}, status: {output?.StatusCode}");
        return output;
    }

    public async Task<DownloadStringResponse> DownloadStringAsync(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<HttpRequestHeaders> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7, CancellationToken? cancellationToken = null, bool getContent = true)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var output = await DownloadStringAsync(url, redirectUrlGetFunc, jsCookieGetFunc, referer, headerSetter, contentType, throwExceptionOnErrorResponse, maxRedirectDepth, 0, cancellationToken, getContent);
        sw.Stop();
        logger.Info($"Call to {url} completed in {sw.Elapsed}, status: {output?.StatusCode}");
        return output;
    }

    private async Task<DownloadStringResponse> DownloadStringAsync(string url, Func<string, string, string> redirectUrlGetFunc, Func<string, CookieCollection> jsCookieGetFunc, string referer, Action<HttpRequestHeaders> headerSetter, string contentType, bool throwExceptionOnErrorResponse, int maxRedirectDepth, int depth, CancellationToken? cancellationToken, bool getContent)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (UserAgent != null)
            request.Headers.UserAgent.TryParseAdd(UserAgent);

        if (Accept != null)
            request.Headers.Accept.TryParseAdd(Accept);

        if (!string.IsNullOrEmpty(referer))
            request.Headers.Referrer = new Uri(referer);

        headerSetter?.Invoke(request.Headers);

        if (contentType != null)
            request.Headers.AddInvalid("Content-Type", contentType);

        HttpStatusCode statusCode;
        string responseUrl;
        string responseContent = null;
        string redirectUrl = null;

        HttpResponseMessage response;
        try
        {
            if (cancellationToken.HasValue)
                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken.Value);
            else
                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        }
        catch (HttpRequestException webex)
        {
            logger.Info(webex, "Error getting response from " + url);

            if (throwExceptionOnErrorResponse)
                throw;

            return null;
        }

        using (response)
        {
            statusCode = response.StatusCode;
            responseUrl = response.RequestMessage.RequestUri.ToString();
            if (HttpRedirectStatusCodes.Contains(response.StatusCode))
            {
                redirectUrl = response.Headers.Location.ToString();
                if (!string.IsNullOrWhiteSpace(redirectUrl))
                    redirectUrl = new Uri(new Uri(url), redirectUrl).AbsoluteUri;
            }

            if (getContent)
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                {
                    responseContent = await reader.ReadToEndAsync();
                }
            }
        }

        if (responseContent != null)
        {
            var jsCookies = jsCookieGetFunc?.Invoke(responseContent);
            Combine(Cookies, jsCookies);
        }

        redirectUrl = redirectUrl ?? redirectUrlGetFunc?.Invoke(url, responseContent);
        if (redirectUrl != null)
        {
            if (depth > maxRedirectDepth)
                return new DownloadStringResponse(redirectUrl, null, statusCode);

            var redirectOutput = await DownloadStringAsync(redirectUrl, redirectUrlGetFunc, jsCookieGetFunc, referer: url, headerSetter, contentType: null, throwExceptionOnErrorResponse, maxRedirectDepth, depth + 1, cancellationToken, getContent);
            return redirectOutput;
        }
        else
        {
            return new DownloadStringResponse(responseUrl, responseContent, statusCode);
        }
    }

    private void Combine(CookieContainer a, CookieCollection b)
    {
        lock (cookieLock)
        {
            if (a == null || a.Count == 0) return;
            if (b == null || b.Count == 0) return;

            a.AddCookies(b.Cast<Cookie>());
        }
    }

    private object cookieLock = new object();
}

public static class HttpRequestHeaderExtensionMethods
{
    public static void Set<T>(this HttpHeaderValueCollection<T> header, string value) where T : class
    {
        header.Clear();
        header.ParseAdd(value);
    }

    public static void AddInvalid(this HttpRequestHeaders headers, string header, string value)
    {
        var invalidHeadersField = typeof(HttpHeaders).GetField("invalidHeaders", BindingFlags.NonPublic | BindingFlags.Instance);
        var invalidHeaders = (HashSet<string>)invalidHeadersField.GetValue(headers);
        invalidHeaders?.Remove(header);
        headers.Add(header, value);
    }
}

/// <summary>
/// Taken from https://stackoverflow.com/a/15588878/1622598
/// </summary>
internal class DangerouslySimpleCookieContainer : DelegatingHandler
{
    internal DangerouslySimpleCookieContainer(CookieContainer cookieContainer = null)
    {
        this.Container = cookieContainer ?? new CookieContainer();
    }

    internal DangerouslySimpleCookieContainer(HttpMessageHandler innerHandler, CookieContainer cookieContainer = null)
        : base(innerHandler)
    {
        this.Container = cookieContainer ?? new CookieContainer();
    }

    public CookieContainer Container { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        this.Container.ApplyCookies(request);
        var response = await base.SendAsync(request, cancellationToken);
        this.Container.SetCookies(response);
        return response;
    }
}

public static class CookieContainerExtensions
{
    internal static void SetCookies(this CookieContainer container, HttpResponseMessage response, Uri requestUri = null)
    {
        if (container is null)
            throw new ArgumentNullException(nameof(container));

        if (response is null)
            throw new ArgumentNullException(nameof(response));

        if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> cookieHeaders))
        {
            foreach (string cookie in cookieHeaders)
            {
                container.SetCookies(requestUri ?? response.RequestMessage.RequestUri, cookie);
            }
        }
    }

    internal static void ApplyCookies(this CookieContainer container, HttpRequestMessage request)
    {
        if (container is null)
            throw new ArgumentNullException(nameof(container));

        if (request is null)
            throw new ArgumentNullException(nameof(request));

        string cookieHeader = container.GetCookieHeader(request.RequestUri);
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }
    }

    public static void AddCookies(this CookieContainer container, IEnumerable<Cookie> cookies)
    {
        foreach (var c in cookies)
            container.Add(c);
    }

    public static void AddCookies(this CookieContainer container, IEnumerable<HttpCookie> httpCookies)
    {
        container.AddCookies(httpCookies.Select(ToCookie));
    }

    private static Cookie ToCookie(HttpCookie cookie)
    {
        var output = new Cookie
        {
            Domain = cookie.Domain,
            HttpOnly = cookie.HttpOnly,
            Path = cookie.Path,
            Name = cookie.Name,
            Value = cookie.Value,
            Secure = cookie.Secure,
        };

        if (cookie.Expires.HasValue)
            output.Expires = cookie.Expires.Value;

        return output;
    }
}