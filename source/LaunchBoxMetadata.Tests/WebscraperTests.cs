using PlayniteExtensions.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LaunchBoxMetadata.Tests
{
    public class WebscraperTests
    {
        [Fact]
        public void ScrapesRE4()
        {
            var downloader = new FakeWebDownloader("https://gamesdb.launchbox-app.com/games/images/3712", "Resident Evil 4 Images.html");
            downloader.AddRedirect("https://gamesdb.launchbox-app.com/games/dbid/3928/", "https://gamesdb.launchbox-app.com/games/details/3712");
            var scraper = new LaunchBoxWebscraper(downloader);
            var url = scraper.GetLaunchBoxGamesDatabaseUrl("3928");
            var images = scraper.GetGameImageDetails(url).ToList();
            Assert.NotEmpty(images);
            Assert.Equal(47, images.Count);
            var first = images.First();
            Assert.Equal("https://images.launchbox-app.com/40bafdaa-5086-4fdc-b150-6cd0edb7f6a4.jpg", first.Url);
            Assert.Equal("https://images.launchbox-app.com/9e94a855-290e-4153-8faa-f06b96f79d3f.jpg", first.ThumbnailUrl);
            Assert.Equal(1920, first.Width);
            Assert.Equal(1080, first.Height);
            Assert.Equal("Fanart - Background", first.Type);
        }

        [Fact]
        public void ScrapesMarioAndLuigi()
        {
            var downloader = new FakeWebDownloader("https://gamesdb.launchbox-app.com/games/images/6399", "Mario & Luigi Superstar Saga Images.html");
            downloader.AddRedirect("https://gamesdb.launchbox-app.com/games/dbid/6691/", "https://gamesdb.launchbox-app.com/games/details/6399");
            var scraper = new LaunchBoxWebscraper(downloader);
            var url = scraper.GetLaunchBoxGamesDatabaseUrl("6691");
            var images = scraper.GetGameImageDetails(url).ToList();
            Assert.NotEmpty(images);
            Assert.Equal(28, images.Count);
            var first = images.First();
            Assert.Equal("https://images.launchbox-app.com/70592a60-f531-4473-ad4c-1a2d032a786c.jpg", first.Url);
            Assert.Equal("https://images.launchbox-app.com/cc9ba942-76b7-43b9-8dfd-07598cc277a2.jpg", first.ThumbnailUrl);
            Assert.Equal(1535, first.Width);
            Assert.Equal(1535, first.Height);
            Assert.Equal("Box - Front", first.Type);
        }
    }
}
