using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GamersGateLibrary.Tests
{
    public class GamersGateScraperTests
    {
        [Fact]
        public void ScrapingOrderPageReturnsOrderUrls()
        {
            var downloader = new FakeWebViewWrapper("https://www.gamersgate.com/account/orders/", "orders_page1.html");
            var scraper = new GamersGateScraper();

            var result = scraper.GetOrderUrls(downloader, 1, out _);

            Assert.NotEmpty(result);
        }

        [Fact]
        public void ScrapingAllOrderUrlsReturnsMoreThanTheFirstPage()
        {
            var downloader = new FakeWebViewWrapper(new Dictionary<string, string> {
                { "https://www.gamersgate.com/account/orders/", "orders_page1.html" },
                { "https://www.gamersgate.com/account/orders/?page=2", "orders_page2.html" },
            });
            var scraper = new GamersGateScraper();

            var firstPageResult = scraper.GetOrderUrls(downloader, 1, out _);
            var allResult = scraper.GetAllOrderUrls(downloader);

            Assert.NotEmpty(firstPageResult);
            Assert.NotEmpty(allResult);
            Assert.True(allResult.Count() > firstPageResult.Count());
        }

        [Fact]
        public void ScrapingOrderReturnsCorrectly()
        {
            var downloader = new FakeWebViewWrapper("https://www.gamersgate.com/account/orders/100000003/", "order_100000003.html");
            var scraper = new GamersGateScraper();

            var games = scraper.GetGamesFromOrder(downloader, "https://www.gamersgate.com/account/orders/100000003/");

            Assert.NotEmpty(games);
            Assert.True(games.All(g => g.OrderId == 100000003));

            Assert.DoesNotContain(games, g => g.Title == "Metro 2033");
            Assert.Contains(games, g => g.Title == "Jamestown: Legend of the Lost Colony");
            Assert.Contains(games, g => g.Title == "Cursed Mountain");

            var jamestown = games.Single(g => g.Title == "Jamestown: Legend of the Lost Colony");
            Assert.Null(jamestown.Key);
            Assert.True(jamestown.UnrevealedKey);
            Assert.Equal("drmfree", jamestown.DRM);
            Assert.Equal(2, jamestown.DownloadUrls.Count);
            Assert.Equal("https://gamersgatep.imgix.net/3/3/0/29ed435815748387d4cab6c0b0b7a468933e2033.jpg?", jamestown.CoverImageUrl);
            Assert.Contains(jamestown.DownloadUrls, u => u.Url == "https://www.gamersgate.com/download/34101");

            var cursedMountain = games.Single(g => g.Title == "Cursed Mountain");
            Assert.Equal("XEJI-78AI-0BGQ-12GM-GHYZ", cursedMountain.Key);
            Assert.False(cursedMountain.UnrevealedKey);
            Assert.Equal("securom", cursedMountain.DRM);
            Assert.Equal(6, cursedMountain.DownloadUrls.Count);
            Assert.Equal("https://gamersgatep.imgix.net/1/8/5/e79e689f897f95bbda92a86a9c71d76b9ad33581.jpg?", cursedMountain.CoverImageUrl);
            Assert.Contains(cursedMountain.DownloadUrls, u => u.Url == "https://www.gamersgate.com/download/14421");
        }
    }
}
