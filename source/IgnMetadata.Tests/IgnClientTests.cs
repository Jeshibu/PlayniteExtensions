using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IgnMetadata.Tests
{
    public class IgnClientTests
    {
        [Fact]
        public void ActualWebRequest()
        {
            var downloader = new WebDownloader();
            IgnClient client = new IgnClient(downloader);
            var result = client.Search("fear");
            Assert.NotEmpty(result);
        }
    }
}
