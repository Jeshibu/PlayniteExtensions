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

        public virtual DownloadStringResponse DownloadString(string url, Func<string, string, string> redirectUrlGetFunc = null, Func<string,CookieCollection> jsCookieGetFunc = null)
        {
            CalledUrls.Add(url);
            if (FilesByUrl.TryGetValue(url, out string filePath))
                return new DownloadStringResponse(url, File.ReadAllText(filePath));
            else
                throw new Exception($"Url not accounted for: {url}");
        }
    }
}