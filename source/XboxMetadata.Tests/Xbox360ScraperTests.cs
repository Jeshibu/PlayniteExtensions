using Moq;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System;
using System.Collections.Generic;
using XboxMetadata.Scrapers;
using Xunit;

namespace XboxMetadata.Tests;

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
            { $"https://marketplace.xbox.com/{settings.Market}/Product/Gears-of-War/66acd000-77fe-1000-9115-d8024d5307d5?cid=search", "x360 en-us gears of war details.html" }
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

    [Fact]
    public void FetchesDOAX2()
    {
        var options = new MetadataRequestOptions(new Game("DEAD OR ALIVE Xtreme 2"), backgroundDownload: true);

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
        settings.Market = "en-US";

        var downloader = new FakeWebDownloader(new Dictionary<string, string>
        {
            { $"https://marketplace.xbox.com/{settings.Market}/Search?query={query}&DownloadType=Game", "x360 en-us dead or alive xtreme 2 search.html" },
            { $"https://marketplace.xbox.com/{settings.Market}/Product/DEAD-OR-ALIVE-Xtreme-2/66acd000-77fe-1000-9115-d802544307d2?cid=search", "x360 en-us dead or alive xtreme 2 details.html" }
        });

        var scraper = new Xbox360Scraper(downloader, new PlatformUtility(playniteApi.Object));
        var scraperManager = new ScraperManager(new[] { scraper });

        var metadataProvider = new XboxMetadataProvider(options, settings, playniteApi.Object, scraperManager);

        var args = new GetMetadataFieldArgs();

        Assert.Equal(options.GameData.Name, metadataProvider.GetName(args));
    }
}
