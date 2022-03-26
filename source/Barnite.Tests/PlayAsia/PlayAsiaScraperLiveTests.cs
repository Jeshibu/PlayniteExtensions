using Barnite.Scrapers;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Barnite.Tests.PlayAsia
{
    public class PlayAsiaScraperLiveTests
    {
        [Fact]
        public void ScrapingCyberpunk2077ReturnsCorrectMetadata()
        {
            var platformSpecIds = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Xbox One", "xbox_one" },
                { "Xbox Series X", "xbox_series" },
            };

            var scraper = new PlayAsiaScraper(new PlatformUtility(platformSpecIds), new Webclient());

            var data = scraper.GetMetadataFromBarcode("5902367640767");

            Assert.NotNull(data);
            Assert.Equal("Cyberpunk 2077", data.Name);
            Assert.Contains(new MetadataSpecProperty("xbox_one"), data.Platforms);
            Assert.Contains(new MetadataSpecProperty("xbox_series"), data.Platforms);
            Assert.Equal("https://s.pacn.ws/1500/x4/cyberpunk-2077-multilanguage-596379.10.jpg", data.CoverImage.Path);
        }
    }
}
