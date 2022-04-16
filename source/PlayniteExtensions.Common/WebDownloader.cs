using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using Playnite.SDK;
using System.Threading;

namespace PlayniteExtensions.Common
{
    public delegate void DownloadProgressCallback(long downloadedBytes, long totalBytes);
    public interface IWebDownloader
    {
        /// <summary>
        /// The total collection of cookies used both as input for requests and output for responses
        /// </summary>
        CookieCollection Cookies { get; }
        DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Dictionary<string, string> customHeaders = null);
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
        private static HttpStatusCode[] HttpRedirectStatusCodes = new[] { HttpStatusCode.Redirect, HttpStatusCode.Moved };

        public int MaxRedirectDepth { get; }
        public CookieCollection Cookies { get; private set; } = new CookieCollection();
        public string UserAgent { get; set; }
        public void SetDefaultUserAgent(IPlayniteAPI playniteAPI)
        {
            SetDefaultUserAgent(playniteAPI.ApplicationInfo.ApplicationVersion.ToString(2));
        }

        public void SetDefaultUserAgent(string playniteVersion)
        {
            UserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36 Playnite/" + playniteVersion;
        }

        public WebDownloader(int maxRedirectDepth = 7)
        {
            MaxRedirectDepth = maxRedirectDepth;
            SetDefaultUserAgent("9.16");
        }

        public WebDownloader(IPlayniteAPI playniteAPI, int maxRedirectDepth = 7) : this(maxRedirectDepth)
        {
            SetDefaultUserAgent(playniteAPI);
        }

        public DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Dictionary<string, string> customHeaders = null)
        {
            return DownloadString(url, redirectUrlGetFunc, jsCookieGetFunc, referer, customHeaders, 0);
        }

        private DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Dictionary<string, string> customHeaders = null, int depth = 0)
        {
            var uri = new Uri(url);
            var request = WebRequest.CreateHttp(uri);
            request.UserAgent = UserAgent;
            request.AllowAutoRedirect = false; //auto-redirect buries response cookies
            if (Cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(Cookies);
            }

            if (!string.IsNullOrEmpty(referer))
                request.Referer = referer;

            if (customHeaders != null)
            {
                foreach (var kvp in customHeaders)
                {
                    request.Headers[kvp.Key] = kvp.Value;
                }
            }

            HttpStatusCode statusCode;
            string responseUrl;
            string responseContent;
            string redirectUrl = null;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                Cookies = Combine(Cookies, response.Cookies);
                statusCode = response.StatusCode;
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

                var redirectOutput = DownloadString(redirectUrl, redirectUrlGetFunc, jsCookieGetFunc, referer: url, customHeaders, depth + 1);
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

        private static CookieCollection Combine(CookieCollection a, CookieCollection b)
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
