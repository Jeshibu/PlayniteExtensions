using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Barnite.Tests.OGDB
{
    public class OgdbScraperTests
    {
        [Fact]
        public void ScrapingGodOfWarAscensionsReturnsCorrectMetadata()
        {
            var stringDownloader = new FakeWebDownloader();
            stringDownloader.FilesByUrl.Add("https://ogdb.eu/index.php?section=simplesearchresults&searchstring=711719230151&how=AND", "./OGDB/gowa_search.html");
            stringDownloader.FilesByUrl.Add("https://ogdb.eu/index.php?section=game&gameid=136487", "./OGDB/gowa_details.html");

            var scraper = new OgdbScraper();
            scraper.Initialize(new PlatformUtility("Sony Playstation 3", "sony_playstation3"), stringDownloader);

            var data = scraper.GetMetadataFromBarcode("711719230151");

            Assert.Equal("God of War: Ascension", data.Name);
            Assert.Equal(new MetadataSpecProperty("sony_playstation3"), data.Platforms.Single());
            Assert.Equal("https://ogdb.eu/imageview.php?image_id=268978&limit=400", data.CoverImage.Path);
            Assert.Equal(new ReleaseDate(2013, 03, 12), data.ReleaseDate);
            Assert.Equal(new MetadataNameProperty("Santa Monica Studio, L.L.C."), data.Developers.Single());
            Assert.Equal(new MetadataNameProperty("SONY Computer Entertainment Europe, Ltd."), data.Publishers.Single());
            Assert.Contains(data.Links, l => l.Name == scraper.Name);
            Assert.Equal(2, stringDownloader.CalledUrls.Count);
        }

        [Fact]
        public void ScrapingDeusExReturnsCorrectMetadata()
        {
            var stringDownloader = new FakeWebDownloader();
            stringDownloader.FilesByUrl.Add("https://ogdb.eu/index.php?section=simplesearchresults&searchstring=788687107112&how=AND", "./OGDB/deusex_search.html");
            stringDownloader.FilesByUrl.Add("https://ogdb.eu/index.php?section=game&gameid=42819", "./OGDB/deusex_details.html");

            var scraper = new OgdbScraper();
            scraper.Initialize(new PlatformUtility(new Dictionary<string,string[]>()), stringDownloader);

            var data = scraper.GetMetadataFromBarcode("788687107112");

            Assert.Equal("Deus Ex", data.Name);
            Assert.Equal(new MetadataSpecProperty("pc_windows"), data.Platforms.Single());
            Assert.Equal("https://ogdb.eu/imageview.php?image_id=72411&limit=400", data.CoverImage.Path);
            Assert.Equal(new ReleaseDate(2000), data.ReleaseDate);
            Assert.Contains(new MetadataNameProperty("ION Storm Austin, L.L.P."), data.Developers);
            Assert.Contains(new MetadataNameProperty("Epic Games, Inc."), data.Developers);
            Assert.Equal(2, data.Developers.Count);
            Assert.Equal(new MetadataNameProperty("Eidos Interactive, Inc."), data.Publishers.Single());
            Assert.Contains(data.Links, l => l.Name == scraper.Name);
            Assert.Equal(2, stringDownloader.CalledUrls.Count);
        }
    }
}