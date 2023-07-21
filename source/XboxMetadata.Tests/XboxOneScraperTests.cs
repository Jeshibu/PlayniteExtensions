using Moq;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XboxMetadata.Scrapers;
using Xunit;

namespace XboxMetadata.Tests
{
    public class XboxOneScraperTests
    {
        [Fact]
        public async Task SearchOnlyIncludesGames()
        {
            string name = "Sniper Elite 5";
            string query = Uri.EscapeDataString(name);
            var settings = XboxMetadataSettings.GetInitialSettings();
            settings.Market = "en-us";

            var downloader = new FakeWebDownloader($"https://www.microsoft.com/msstoreapiprod/api/autosuggest?market={settings.Market}&sources=xSearch-Products&filter=+ClientType:StoreWeb&counts=20&query={query}", "xbone sniper elite 5 search.json");
            var scraper = new XboxOneScraper(downloader, new PlatformUtility((IPlayniteAPI)null));
            var result = (await scraper.SearchAsync(settings, name)).ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void FetchesSniperElite5()
        {
            var options = new MetadataRequestOptions(new Game("Sniper Elite 5"), backgroundDownload: true);

            var playniteSettingsApi = new Mock<IPlayniteSettingsAPI>(MockBehavior.Strict);
            playniteSettingsApi.SetupGet(a => a.AgeRatingOrgPriority).Returns(AgeRatingOrg.ESRB);

            var database = new Mock<IGameDatabaseAPI>(MockBehavior.Strict);
            database.SetupGet(d => d.Platforms).Returns(new FakeItemCollection<Platform>());

            var notifications = new Mock<INotificationsAPI>(MockBehavior.Loose);

            var playniteApi = new Mock<IPlayniteAPI>(MockBehavior.Strict);
            playniteApi.SetupGet(a => a.ApplicationSettings).Returns(playniteSettingsApi.Object);
            playniteApi.SetupGet(a => a.Notifications).Returns(notifications.Object);
            playniteApi.SetupGet(a => a.Database).Returns(database.Object);

            string query = Uri.EscapeDataString(options.GameData.Name);
            var settings = XboxMetadataSettings.GetInitialSettings();
            settings.Market = "en-us";

            var downloader = new FakeWebDownloader(new Dictionary<string, string>
            {
                { $"https://www.microsoft.com/msstoreapiprod/api/autosuggest?market={settings.Market}&sources=xSearch-Products&filter=+ClientType:StoreWeb&counts=20&query={query}", "xbone sniper elite 5 search.json" },
                { $"https://www.xbox.com/{settings.Market}/games/store/-/9pp8q82h79lc", "xbone sniper elite 5 details.html" }
            });

            var scraper = new XboxOneScraper(downloader, new PlatformUtility(playniteApi.Object));
            var scraperManager = new ScraperManager(new[] { scraper });

            var metadataProvider = new XboxMetadataProvider(options, settings, playniteApi.Object, scraperManager);

            var dev = metadataProvider.GetDevelopers(new GetMetadataFieldArgs());

            Assert.Single(dev, new MetadataNameProperty("Rebellion"));
        }

    }

    public class Xbox360ScraperTests
    {
        [Fact]
        public void FetchesGearsOfWar()
        {
            var options = new MetadataRequestOptions(new Game("Gears of War"), backgroundDownload: true);

            var playniteSettingsApi = new Mock<IPlayniteSettingsAPI>(MockBehavior.Strict);
            playniteSettingsApi.SetupGet(a => a.AgeRatingOrgPriority).Returns(AgeRatingOrg.ESRB);

            var database = new Mock<IGameDatabaseAPI>(MockBehavior.Strict);
            database.SetupGet(d => d.Platforms).Returns(new FakeItemCollection<Platform>());

            var notifications = new Mock<INotificationsAPI>(MockBehavior.Loose);

            var playniteApi = new Mock<IPlayniteAPI>(MockBehavior.Strict);
            playniteApi.SetupGet(a => a.ApplicationSettings).Returns(playniteSettingsApi.Object);
            playniteApi.SetupGet(a => a.Notifications).Returns(notifications.Object);
            playniteApi.SetupGet(a => a.Database).Returns(database.Object);

            string query = Uri.EscapeDataString(options.GameData.Name);
            var settings = XboxMetadataSettings.GetInitialSettings();
            settings.Market = "en-us";

            var downloader = new FakeWebDownloader(new Dictionary<string, string>
            {
                { $"https://marketplace.xbox.com/{settings.Market}/Search?query={query}&DownloadType=Game", "x360 en-us gears of war search.html" },
                { $"https://marketplace.xbox.com/{settings.Market}/Product/-/66acd000-77fe-1000-9115-d8024d5307d5", "x360 en-us gears of war details.html" }
            });

            var scraper = new Xbox360Scraper(downloader, new PlatformUtility(playniteApi.Object));
            var scraperManager = new ScraperManager(new[] { scraper });

            var metadataProvider = new XboxMetadataProvider(options, settings, playniteApi.Object, scraperManager);

            var args = new GetMetadataFieldArgs();

            Assert.Equal("Gears of War", metadataProvider.GetName(args));
            Assert.Equal(90, metadataProvider.GetCommunityScore(args));
            Assert.Equal("https://download-ssl.xbox.com/content/images/66acd000-77fe-1000-9115-d8024d5307d5/1033/boxartlg.jpg", metadataProvider.GetCoverImage(args)?.Path);
            Assert.Equal("https://download-ssl.xbox.com/content/images/66acd000-77fe-1000-9115-d8024d5307d5/1033/screenlg1.jpg", metadataProvider.GetBackgroundImage(args)?.Path);
        }
    }
}
