using Moq;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XboxMetadata.Scrapers;
using Xunit;

namespace XboxMetadata.Tests;

public class XboxOneScraperTests
{
    [Fact]
    public async Task SearchOnlyIncludesGames()
    {
        string name = "Sniper Elite 5";
        var settings = XboxMetadataSettings.GetInitialSettings();
        settings.Market = "en-us";

        var downloader = new FakeWebDownloader(XboxOneScraper.GetSearchUrl(settings.Market, name), "xbone sniper elite 5 search.json");
        var scraper = new XboxOneScraper(downloader, new PlatformUtility((IPlayniteAPI)null));
        var result = (await scraper.SearchAsync(settings, name)).Where(r => !r.Title.EndsWith(" Edition")).ToList();
        Assert.Single(result);
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

        var settings = XboxMetadataSettings.GetInitialSettings();
        settings.Market = "en-us";

        var detailsUrl = $"https://www.xbox.com/{settings.Market}/games/store/sniper-elite-5/9pp8q82h79lc";
        var downloader = new FakeWebDownloader(new Dictionary<string, string>
        {
            { XboxOneScraper.GetSearchUrl(settings.Market, options.GameData.Name), "xbone sniper elite 5 search.json" },
            { detailsUrl, "xbone sniper elite 5 details.html" }
        });
        downloader.AddRedirect("https://www.microsoft.com/en-us/store/p/sniper-elite-5/9pp8q82h79lc", detailsUrl);

        var scraper = new XboxOneScraper(downloader, new PlatformUtility(playniteApi.Object));
        var scraperManager = new ScraperManager(new[] { scraper });

        var metadataProvider = new XboxMetadataProvider(options, settings, playniteApi.Object, scraperManager);

        var dev = metadataProvider.GetDevelopers(new GetMetadataFieldArgs());

        Assert.Single(dev, new MetadataNameProperty("Rebellion"));
    }

    [Fact]
    public async Task MicrosoftStorePageParses()
    {
        var content = System.IO.File.ReadAllText("microsoft minesweeper 2019 details.html");
        XboxOneScraper scraper = new XboxOneScraper(null, new PlatformUtility((string)null));
        var settings = new XboxMetadataSettings
        {
            Market = "en-us",
            Cover = new XboxImageSourceSettings
            {
                AspectRatio = AspectRatio.Any,
                MinHeight = 100,
                MinWidth = 100,
                MaxHeight = 1000,
                MaxWidth = 1000,
                Fields = [new CheckboxSetting(ImageSourceField.AppStoreProductImage, true)]
            }
        };
        var response = new DownloadStringResponse("https://www.microsoft.com/somepage", content, System.Net.HttpStatusCode.OK);
        var details = await scraper.GetMicrosoftDotComGameDetailsAsync(settings, "1", response);
        Assert.NotNull(details);
        Assert.Equal("Minesweeper 2019", details.Title);
        Assert.Single(details.Covers);
        var cover = details.Covers.Single();
        Assert.Equal("https://store-images.s-microsoft.com/image/apps.47258.13904116143616172.7fd46af2-310b-4692-bdc5-de4529e4705d.a046addc-a5dc-4549-a6ce-ff53b547e902?mode=scale&q=90&h=270&w=270&background=white", cover.Url);
        Assert.Equal(270, cover.Width);
        Assert.Equal(270, cover.Height);
    }
}
