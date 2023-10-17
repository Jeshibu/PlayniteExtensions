using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Linq;
using Xunit;

namespace Barnite.Tests.VGCollect
{
    public class VGCollectScraperTests
    {
        [Fact]
        public void ScrapingMonsterHunterTriReturnsCorrectMetadata()
        {
            var stringDownloader = new FakeWebDownloader();
            stringDownloader.FilesByUrl.Add("https://html.duckduckgo.com/html/?q=2128490T+site%3Avgcollect.com", "./VGCollect/duckduckgo-monsterhuntertri.html");
            stringDownloader.FilesByUrl.Add("https://vgcollect.com/item/66122", "./VGCollect/vgcollect-monsterhuntertri.html");

            var scraper = new VGCollectScraper();
            scraper.Initialize(new PlatformUtility("Wii", "nintendo_wii"), stringDownloader);

            var data = scraper.GetMetadataFromBarcode("2128490T");

            Assert.Equal("Monster Hunter Tri", data.Name);
            Assert.Equal(new MetadataSpecProperty("nintendo_wii"), data.Platforms.Single());
            Assert.Equal(new MetadataNameProperty("EU"), data.Regions.Single());
            Assert.Equal("https://vgcollect.com/images/front-box-art/66122.jpg", data.CoverImage.Path);
        }
    }
}
