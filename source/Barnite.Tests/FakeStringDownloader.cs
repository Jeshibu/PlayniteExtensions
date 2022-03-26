using Barnite.Scrapers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Barnite.Tests
{
    public class FakeWebclient : IWebclient
    {
        public Dictionary<string, string> FilesByUrl { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public List<string> CalledUrls { get; } = new List<string>();

        public FakeWebclient() { }

        public FakeWebclient(string url, string localFile)
        {
            FilesByUrl.Add(url, localFile);
        }

        public FakeWebclient(Dictionary<string, string> filesByUrl)
        {
            FilesByUrl = filesByUrl;
        }

        public virtual string DownloadString(string url, CookieCollection cookies = null)
        {
            return DownloadString(url, out _, cookies);
        }

        public virtual string DownloadString(string url, out CookieCollection responseCookies, CookieCollection cookies = null)
        {
            responseCookies = new CookieCollection();
            CalledUrls.Add(url);
            if (FilesByUrl.TryGetValue(url, out string filePath))
                return File.ReadAllText(filePath);
            else
                throw new Exception($"Url not accounted for: {url}");
        }
    }
}