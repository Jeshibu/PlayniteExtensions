using Barnite.Scrapers;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace Barnite.Tests
{
    public class FakeWebDownloader : IWebDownloader
    {
        public Dictionary<string, string> FilesByUrl { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
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

        public virtual DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string, CookieCollection> jsCookieGetFunc = null, string referer = null, Dictionary<string, string> customHeaders = null)
        {
            CalledUrls.Add(url);
            if (FilesByUrl.TryGetValue(url, out string filePath))
                return new DownloadStringResponse(url, File.ReadAllText(filePath), HttpStatusCode.OK);
            else
                throw new Exception($"Url not accounted for: {url}");
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
    }
}