using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using Playnite.SDK;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteExtensions.Common
{
    public delegate void DownloadProgressCallback(long downloadedBytes, long totalBytes);
    public interface IWebDownloader
    {
        /// <summary>
        /// The total collection of cookies used both as input for requests and output for responses
        /// </summary>
        CookieCollection Cookies { get; }
        DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<WebHeaderCollection> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7);
        Task<DownloadStringResponse> DownloadStringAsync(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<WebHeaderCollection> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7);
        string DownloadFile(string url, string targetFolder, CancellationToken cancellationToken, DownloadProgressCallback progressCallback = null);
    }

    public class DownloadStringResponse
    {
        public DownloadStringResponse(string responseUrl, string responseContent, HttpStatusCode statusCode)
        {
            ResponseUrl = responseUrl;
            ResponseContent = responseContent;
            StatusCode = statusCode;
        }

        public string ResponseUrl { get; set; }
        public string ResponseContent { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }

    public class WebDownloader : IWebDownloader
    {
        private ILogger logger = LogManager.GetLogger();
        public static HttpStatusCode[] HttpRedirectStatusCodes = new[] { HttpStatusCode.Redirect, HttpStatusCode.Moved, HttpStatusCode.TemporaryRedirect, (HttpStatusCode)308 };

        public CookieCollection Cookies { get; private set; } = new CookieCollection();
        public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0";
        public string Accept { get; set; } = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";

        public WebDownloader()
        {
        }

        public DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<WebHeaderCollection> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var task = DownloadStringAsync(url, redirectUrlGetFunc, jsCookieGetFunc, referer, headerSetter, contentType, throwExceptionOnErrorResponse, maxRedirectDepth, depth: 0);
            task.Wait();
            var output = task.Result;
            sw.Stop();
            logger.Info($"Call to {url} completed in {sw.Elapsed}, status: {output?.StatusCode}");
            return output;
        }

        public async Task<DownloadStringResponse> DownloadStringAsync(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<WebHeaderCollection> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var output = await DownloadStringAsync(url, redirectUrlGetFunc, jsCookieGetFunc, referer, headerSetter, contentType, throwExceptionOnErrorResponse, maxRedirectDepth, depth: 0);
            sw.Stop();
            logger.Info($"Call to {url} completed in {sw.Elapsed}, status: {output?.StatusCode}");
            return output;
        }

        private async Task<DownloadStringResponse> DownloadStringAsync(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Action<WebHeaderCollection> headerSetter = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7, int depth = 0)
        {
            var uri = new Uri(url);
            var request = WebRequest.CreateHttp(uri);

            if (UserAgent != null)
                request.UserAgent = UserAgent;

            if (Accept != null)
                request.Accept = Accept;

            request.AllowAutoRedirect = false; //auto-redirect buries response cookies
            if (Cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(Cookies);
            }

            if (!string.IsNullOrEmpty(referer))
                request.Referer = referer;

            headerSetter?.Invoke(request.Headers);

            if (contentType != null)
                request.ContentType = contentType;

            HttpStatusCode statusCode;
            string responseUrl;
            string responseContent;
            string redirectUrl = null;

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)await request.GetResponseAsync();
            }
            catch (WebException webex)
            {
                logger.Info(webex, "Error getting response from " + url);

                if (throwExceptionOnErrorResponse)
                    throw;

                if (webex.Response is HttpWebResponse)
                    response = (HttpWebResponse)webex.Response;
                else
                    throw;
            }

            using (response)
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                Cookies = Combine(Cookies, response.Cookies);
                statusCode = response.StatusCode;
                responseUrl = response.ResponseUri.AbsoluteUri;
                responseContent = await reader.ReadToEndAsync();
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
                if (depth > maxRedirectDepth)
                    return new DownloadStringResponse(redirectUrl, null, statusCode);

                var redirectOutput = await DownloadStringAsync(redirectUrl, redirectUrlGetFunc, jsCookieGetFunc, referer: url, headerSetter, contentType: null, throwExceptionOnErrorResponse, depth + 1);
                return redirectOutput;
            }
            else
            {
                return new DownloadStringResponse(responseUrl, responseContent, statusCode);
            }
        }

        public string DownloadFile(string url, string targetFolder, CancellationToken cancellationToken, DownloadProgressCallback progressCallback = null)
        {
            var request = WebRequest.CreateHttp(url);
            request.UserAgent = UserAgent;
            request.AllowAutoRedirect = false;
            if (Cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(Cookies);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Cookies = Combine(Cookies, response.Cookies);
                if (!IsFile(response))
                    throw new Exception("Expected file, but found " + response.ContentType);

                string fileName = GetFileName(response);
                string targetFilePath = Path.Combine(targetFolder, fileName);

                using (var stream = response.GetResponseStream())
                using (var writer = File.Create(targetFilePath))
                {
                    long totalBytesWritten = 0;
                    int bytesRead, bufferSize = 8192;
                    byte[] buffer = new byte[bufferSize];
                    while ((bytesRead = stream.Read(buffer, 0, bufferSize)) > 0)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            writer.Dispose();
                            File.Delete(targetFilePath);
                            return null;
                        }

                        writer.Write(buffer, 0, bytesRead);
                        totalBytesWritten += bytesRead;

                        progressCallback?.Invoke(totalBytesWritten, response.ContentLength);
                    }
                    writer.Flush();
                }
                return targetFilePath;
            }
        }

        private static string GetFileName(HttpWebResponse response)
        {
            var contentDispositionString = response.Headers["Content-Disposition"];
            if (contentDispositionString != null)
            {
                var contentDisposition = new System.Net.Mime.ContentDisposition(contentDispositionString);
                if (!string.IsNullOrEmpty(contentDisposition.FileName))
                    return contentDisposition.FileName;
            }
            return Path.GetFileName(response.ResponseUri.AbsoluteUri);
        }

        private static bool IsFile(HttpWebResponse response)
        {
            return response.ContentType.StartsWith("application");
        }

        private CookieCollection Combine(CookieCollection a, CookieCollection b)
        {
            lock (cookieLock)
            {
                if (a == null || a.Count == 0) return b;
                if (b == null || b.Count == 0) return a;

                var c = new CookieCollection { a, b };

                return c;
            }
        }

        private object cookieLock = new object();
    }

}
