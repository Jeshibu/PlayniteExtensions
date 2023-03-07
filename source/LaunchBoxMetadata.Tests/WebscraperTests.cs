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
            var first = images.First();
            Assert.Equal("https://images.launchbox-app.com/40bafdaa-5086-4fdc-b150-6cd0edb7f6a4.jpg", first.Url);
            Assert.Equal("https://images.launchbox-app.com/9e94a855-290e-4153-8faa-f06b96f79d3f.jpg", first.ThumbnailUrl);
            Assert.Equal(1920, first.Width);
            Assert.Equal(1080, first.Height);
            Assert.Equal("Fanart - Background", first.Type);
        }
    }
}
