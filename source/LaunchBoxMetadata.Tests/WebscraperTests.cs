using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace LaunchBoxMetadata.Tests;

public class WebscraperTests
{
    ImageComparer imgComparer = new();

    [Fact]
    public void ScrapesRE4()
    {
        var downloader = new FakeWebDownloader("https://gamesdb.launchbox-app.com/games/details/3712", "Resident Evil 4 Details.html");
        downloader.AddRedirect("https://gamesdb.launchbox-app.com/games/dbid/3928/", "https://gamesdb.launchbox-app.com/games/details/3712");
        var scraper = new LaunchBoxWebScraper(downloader);

        var url = scraper.GetLaunchBoxGamesDatabaseUrl(3928L);
        var images = scraper.GetGameImageDetails(url).ToList();

        Assert.Equal(57, images.Count);
        Assert.Equal(new LaunchBoxImageDetails
        {
            ThumbnailUrl = "https://images.launchbox-app.com//e9f57493-3056-4dad-b936-107053ab832b.png",
            Url = "https://images.launchbox-app.com//bddb8499-3e8d-47e1-bfbc-90f8ccf8dc75.png",
            Type = "Banner",
            Region = null,
            Width = 760,
            Height = 140,
        }, images[0], imgComparer);
        Assert.Equal(new LaunchBoxImageDetails
        {
            ThumbnailUrl = "https://gamesdb-images.launchbox.gg/r2_25c623f2-fd5c-4992-bc24-ecbf79a7fdd5.png",
            Url = "https://gamesdb-images.launchbox.gg/r2_3c8de30f-d410-44f1-bc47-f1c6864ec45d.png",
            Type = "Box - Front",
            Region = "North America",
            Width = 1576,
            Height = 2246,
        }, images[27], imgComparer);
    }

    [Fact]
    public void ScrapesMarioAndLuigi()
    {
        var downloader = new FakeWebDownloader("https://gamesdb.launchbox-app.com/games/details/6399", "Mario & Luigi Superstar Saga Details.html");
        downloader.AddRedirect("https://gamesdb.launchbox-app.com/games/dbid/6691/", "https://gamesdb.launchbox-app.com/games/details/6399");
        var scraper = new LaunchBoxWebScraper(downloader);

        var url = scraper.GetLaunchBoxGamesDatabaseUrl(6691L);
        var images = scraper.GetGameImageDetails(url).ToList();

        Assert.Equal(36, images.Count);
        Assert.Contains(new LaunchBoxImageDetails
        {
            ThumbnailUrl = "https://images.launchbox-app.com//654f5ad2-8a85-4585-8a25-812d539f2899.png",
            Url = "https://images.launchbox-app.com//bed3cd4c-c11a-4d23-b79b-2cc781455074.png",
            Type = "Screenshot - Gameplay",
            Region = null,
            Width = 3360,
            Height = 2240,
        }, images, imgComparer);
    }
}

internal class ImageComparer : EqualityComparer<LaunchBoxImageDetails>
{
    public override bool Equals(LaunchBoxImageDetails x, LaunchBoxImageDetails y)
    {
        return x.Url == y.Url
            && x.ThumbnailUrl == y.ThumbnailUrl
            && x.Type == y.Type
            && x.Region == y.Region
            && x.Width == y.Width;
    }

    public override int GetHashCode(LaunchBoxImageDetails obj)
    {
        return $"{obj.Url}|{obj.ThumbnailUrl}|{obj.Type}|{obj.Region}|{obj.Width}|{obj.Height}".GetHashCode();
    }
}
