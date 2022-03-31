using Barnite.Scrapers;
using Playnite.SDK.Models;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Barnite.Tests.PriceCharting
{
    public class PriceChartingScraperTests
    {
        [Fact]
        public void ScrapingGodOfWarReturnsCorrectMetadata()
        {
            var stringDownloader = new FakeWebclient();
            stringDownloader.FilesByUrl.Add("https://www.pricecharting.com/search-products?category=videogames&q=0711719357476", "./PriceCharting/gow_search.html");
            stringDownloader.FilesByUrl.Add("https://www.pricecharting.com/offers?product=57416", "./PriceCharting/gow_details.html");

            var scraper = new PriceChartingScraper(new PlatformUtility("Playstation 4", "sony_playstation4"), stringDownloader);

            var data = scraper.GetMetadataFromBarcode("0711719357476");

            Assert.Equal("God of War", data.Name);
            Assert.Equal(new MetadataSpecProperty("sony_playstation4"), data.Platforms.Single());
            Assert.Equal("https://commondatastorage.googleapis.com/images.pricecharting.com/AMIfv96gBD8eKKhykpxlo3TZTNYw65pa28xMSZbAJDb1lqbM7U9aB653ksyFUqg3Mv9Y2k1pcz_L1O1aD8PUUsQZMPgY3PO2iO0D6uy9RZpKvwabm7webK0JQLjC4ygH4HreNrhxxfg_wjRnXZ3TvM3CMK3wygIwUA/120.jpg", data.CoverImage?.Path);
            Assert.Contains(data.Links, l => l.Name == scraper.Name);
            Assert.Equal(2, stringDownloader.CalledUrls.Count);
        }
    }
}