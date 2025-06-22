using PlayniteExtensions.Tests.Common;
using System.Linq;
using Xunit;

namespace LaunchBoxMetadata.Tests;

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
        Assert.Equal(51, images.Count);
        var first = images.First();
        Assert.Equal("https://images.launchbox-app.com/02c16fe4-39cb-4c0a-81bd-e707afc0634e.png", first.Url);
        Assert.Equal("https://images.launchbox-app.com/fee46f13-8a82-40b0-8d2b-8a47cd3f2399.png", first.ThumbnailUrl);
        Assert.Equal(1080, first.Width);
        Assert.Equal(1531, first.Height);
        Assert.Equal("Box - Front", first.Type);
        Assert.Equal("North America", first.Region);
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
        Assert.Equal(34, images.Count);
        var first = images.First();
        Assert.Equal("https://images.launchbox-app.com/70592a60-f531-4473-ad4c-1a2d032a786c.jpg", first.Url);
        Assert.Equal("https://images.launchbox-app.com/cc9ba942-76b7-43b9-8dfd-07598cc277a2.jpg", first.ThumbnailUrl);
        Assert.Equal(1535, first.Width);
        Assert.Equal(1535, first.Height);
        Assert.Equal("Box - Front", first.Type);
        Assert.Equal("North America", first.Region);
    }
}
