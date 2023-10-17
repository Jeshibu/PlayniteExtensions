using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;

namespace GamersGateLibrary.Tests
{
    class FakeWebViewWrapper : FakeWebDownloader, IWebViewWrapper
    {
        public FakeWebViewWrapper()
        {
        }

        public FakeWebViewWrapper(Dictionary<string, string> filesByUrl) : base(filesByUrl)
        {
        }

        public FakeWebViewWrapper(string url, string localFile) : base(url, localFile)
        {
        }

        public void Dispose()
        {
        }

        public string DownloadPageSource(string targetUrl, int timeoutSeconds = 60)
        {
            return base.DownloadString(targetUrl).ResponseContent;
        }
    }
}
