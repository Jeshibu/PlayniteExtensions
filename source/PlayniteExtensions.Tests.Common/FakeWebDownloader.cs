using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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

        public CookieCollection Cookies { get; } = new CookieCollection();

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

        public virtual async Task<DownloadStringResponse> DownloadStringAsync(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Dictionary<string, string> customHeaders = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxResponseDepth = 7)
        {
            return DownloadString(url, redirectUrlGetFunc, jsCookieGetFunc, referer, customHeaders, contentType: null, throwExceptionOnErrorResponse, maxResponseDepth);
        }

        public string DownloadFile(string url, string targetFolder)
        {
            CalledUrls.Add(url);
            if (FilesByUrl.TryGetValue(url, out string filePath))
            {
                string targetPath = Path.Combine(targetFolder, Path.GetFileName(filePath));
                File.Copy(filePath, targetPath, overwrite: true);
                return targetPath;
            }
            else
            {
                throw new Exception($"Url not accounted for: {url}");
            }
        }

        public string DownloadFile(string url, string targetFolder, CancellationToken cancellationToken, DownloadProgressCallback progressCallback = null)
        {
            throw new NotImplementedException();
        }

        public DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Dictionary<string, string> customHeaders = null, string contentType = null, bool throwExceptionOnErrorResponse = true, int maxRedirectDepth = 7)
        {
            CalledUrls.Add(url);
            if (FilesByUrl.TryGetValue(url, out string filePath))
                return new DownloadStringResponse(url, File.ReadAllText(filePath), HttpStatusCode.OK);

            if (RedirectsByUrl.TryGetValue(url, out Redirect redir))
            {
                if (maxRedirectDepth < redir.Depth)
                    return new DownloadStringResponse(redir.RedirectUrl, null, HttpStatusCode.Redirect);
                else
                    return DownloadString(redir.RedirectUrl, redirectUrlGetFunc, jsCookieGetFunc, referer, customHeaders, contentType, throwExceptionOnErrorResponse, maxRedirectDepth);
            }

            throw new Exception($"Url not accounted for: {url}");
        }
    }
}