using Barnite.Scrapers;
using Playnite.SDK.Models;
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
            var stringDownloader = new FakeWebclient();
            stringDownloader.FilesByUrl.Add("https://ogdb.eu/index.php?section=simplesearchresults&searchstring=711719230151&how=AND", "./OGDB/gowa_search.html");
            stringDownloader.FilesByUrl.Add("https://ogdb.eu/index.php?section=game&gameid=136487", "./OGDB/gowa_details.html");

            var scraper = new OgdbScraper(new PlatformUtility("Sony Playstation 3", "sony_playstation3"), stringDownloader);

            var data = scraper.GetMetadataFromBarcode("711719230151");

            Assert.Equal("God of War: Ascension", data.Name);
            Assert.Equal(new MetadataSpecProperty("sony_playstation3"), data.Platforms.Single());
            Assert.Equal("https://ogdb.eu/imageview.php?image_id=268978&limit=400", data.CoverImage.Path);
            Assert.Equal(new ReleaseDate(2013, 03, 12), data.ReleaseDate);
            Assert.Equal(new MetadataNameProperty("Santa Monica Studio, L.L.C."), data.Developers.Single());
            Assert.Equal(new MetadataNameProperty("SONY Computer Entertainment Europe, Ltd."), data.Publishers.Single());
            Assert.Equal(2, stringDownloader.CalledUrls.Count);
        }
    }
}