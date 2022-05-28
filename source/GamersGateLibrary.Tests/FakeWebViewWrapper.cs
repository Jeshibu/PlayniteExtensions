using PlayniteExtensions.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public string DownloadPageSource(string targetUrl)
        {
            return base.DownloadString(targetUrl).ResponseContent;
        }
    }
}
