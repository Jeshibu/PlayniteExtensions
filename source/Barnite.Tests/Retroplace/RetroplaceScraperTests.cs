using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Linq;
using Xunit;

namespace Barnite.Tests.Retroplace;

public class RetroplaceScraperTests
{
    [Fact]
    public void ScrapingKillzoneReturnsCorrectMetadata()
    {
        var stringDownloader = new FakeWebDownloader();
        stringDownloader.FilesByUrl.Add("https://www.retroplace.com/en/games/marketplace?barcode=711719136415", "./Retroplace/killzone_search.html");
        stringDownloader.FilesByUrl.Add("https://www.retroplace.com/en/games/71503--77362--killzone", "./Retroplace/killzone.html");

        var scraper = new RetroplaceScraper();
        scraper.Initialize(new PlatformUtility("Sony Playstation 2", "sony_playstation2"), stringDownloader);

        var data = scraper.GetMetadataFromBarcode("711719136415");

        Assert.Equal("Killzone", data.Name);
        Assert.Equal(new MetadataSpecProperty("sony_playstation2"), data.Platforms.Single());
        Assert.Equal(new MetadataNameProperty("Guerrilla"), data.Developers.Single());
        Assert.Equal(new MetadataNameProperty("SCEE"), data.Publishers.Single());
        Assert.Contains(new MetadataNameProperty("Action"), data.Genres);
        Assert.Contains(new MetadataNameProperty("Shooter"), data.Genres);
        Assert.Contains(new MetadataNameProperty("First-Person"), data.Genres);
        Assert.Contains(new MetadataNameProperty("Arcade"), data.Genres);
        Assert.Equal(new ReleaseDate(2005, 6, 10), data.ReleaseDate);
    }
}
