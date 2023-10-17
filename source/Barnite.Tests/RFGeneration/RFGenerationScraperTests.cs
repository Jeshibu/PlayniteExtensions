using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Linq;
using Xunit;

namespace Barnite.Tests.RFGeneration
{
    public class RFGenerationScraperTests
    {
        [Fact]
        public void ScrapingMonsterHunterTriReturnsCorrectMetadata()
        {
            var stringDownloader = new FakeWebDownloader();
            stringDownloader.FilesByUrl.Add("https://html.duckduckgo.com/html/?q=045496366933+site%3Arfgeneration.com", "./RFGeneration/ddg-pikmin.html");
            stringDownloader.FilesByUrl.Add("https://www.rfgeneration.com/cgi-bin/getinfo.pl?ID=E-132-S-01050-A", "./RFGeneration/rfg-pikmin.html");

            var scraper = new RFGenerationScraper();
            scraper.Initialize(new PlatformUtility("Nintendo Wii", "nintendo_wii"), stringDownloader);

            var data = scraper.GetMetadataFromBarcode("045496366933");

            Assert.Equal("Pikmin [New Play Control!]", data.Name);
            Assert.Equal(new MetadataSpecProperty("nintendo_wii"), data.Platforms.Single());
            Assert.Contains(new MetadataNameProperty("Belgium"), data.Regions);
            Assert.Contains(new MetadataNameProperty("Netherlands"), data.Regions);
            Assert.Equal(new MetadataNameProperty("Nintendo"), data.Developers.Single());
            Assert.Equal(new MetadataNameProperty("Nintendo"), data.Publishers.Single());
            Assert.Equal(new ReleaseDate(2009), data.ReleaseDate);
        }
    }
}
