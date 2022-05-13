using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
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
    public class PlayAsiaScraperTests
    {
        [Fact]
        public void ScrapingAstralChainReturnsCorrectMetadata()
        {
            var stringDownloader = new FakeWebDownloader("https://www.play-asia.com/search/045496424671", "./PlayAsia/astralchain.html");

            var scraper = new PlayAsiaScraper();
            scraper.Initialize(new PlatformUtility("Nintendo Switch", "nintendo_switch"), stringDownloader);

            var data = scraper.GetMetadataFromBarcode("045496424671");

            Assert.Equal("Astral Chain", data.Name);
            Assert.Equal(new MetadataSpecProperty("nintendo_switch"), data.Platforms.Single());
            Assert.Equal("https://s.pacn.ws/1500/wk/astral-chain-586101.11.jpg", data.CoverImage.Path);
        }

        [Fact]
        public void ScrapingCyberpunk2077ReturnsCorrectMetadata()
        {
            var stringDownloader = new FakeWebDownloader("https://www.play-asia.com/search/5902367640767", "./PlayAsia/cyberpunk2077.html");

            var platformSpecIds = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Xbox One", new[]{ "xbox_one" } },
                { "Xbox Series X", new[]{ "xbox_series" } },
            };

            var scraper = new PlayAsiaScraper();
            scraper.Initialize(new PlatformUtility(platformSpecIds), stringDownloader);

            var data = scraper.GetMetadataFromBarcode("5902367640767");

            Assert.Equal("Cyberpunk 2077", data.Name);
            Assert.Contains(new MetadataSpecProperty("xbox_one"), data.Platforms);
            Assert.Contains(new MetadataSpecProperty("xbox_series"), data.Platforms);
            Assert.Equal("https://s.pacn.ws/1500/x4/cyberpunk-2077-multilanguage-596379.10.jpg", data.CoverImage.Path);
        }
    }
}
