using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Linq;
using Xunit;

namespace Barnite.Tests.UpcItemDb;

public class UpcItemDbScraperTests
{
    [Fact]
    public void ScrapingPikminReturnsCorrectMetadata()
    {
        var stringDownloader = new FakeWebDownloader();
        stringDownloader.FilesByUrl.Add("https://api.upcitemdb.com/prod/trial/lookup?upc=045496901431", "./UpcItemDb/pikmin.json");

        var scraper = new UpcItemDbScraper();
        scraper.Initialize(new PlatformUtility("Wii", "nintendo_wii"), stringDownloader);

        var data = scraper.GetMetadataFromBarcode("045496901431");

        Assert.Equal("Pikmin", data.Name);
        Assert.Equal(new MetadataSpecProperty("nintendo_wii"), data.Platforms.Single());
        Assert.Equal("Life under a microscope fantasy worldUnique experienceAdventurePuzzle elementsControl Captain Olimar, lovable little astronaut", data.Description);
        Assert.Equal("https://i5.walmartimages.com/asr/f5809824-202f-4313-bfdb-af0dabc98e43_1.61e3aa299a6fc451343e05ef0c4bd711.jpeg?odnHeight=450&odnWidth=450&odnBg=ffffff", data.CoverImage.Path);
    }

    [Fact]
    public void ScrapingNinjaGaidenSigmaReturnsCorrectMetadata()
    {
        var stringDownloader = new FakeWebDownloader();
        stringDownloader.FilesByUrl.Add("https://api.upcitemdb.com/prod/trial/lookup?upc=0018946010595", "./UpcItemDb/ngs.json");

        var scraper = new UpcItemDbScraper();
        scraper.Initialize(new PlatformUtility("PS3", "sony_playstation3"), stringDownloader);

        var data = scraper.GetMetadataFromBarcode("0018946010595");

        Assert.Equal("Ninja Gaiden Sigma", data.Name);
        Assert.Empty(data.Platforms);
        Assert.Equal("Ninja Gaiden Sigma puts realistic battle and acrobatic ninja moves at your fingertips. As Ryu Hayabusa, you are seek revenge after your clan is massacred by the Vigor Empire. All you have are your wits, sword and skills. Your weapons & combat skills are great, but only time will tell if they re enough to beat the Holy Emperor and reclaim the magic sword named  Ryuken . Upload your Karma scores on the Playstation network & compare leader boards SKU:ADIB001BP4JY6", data.Description);
    }

    [Fact]
    public void ScrapingNinjaGaidenSigma2ReturnsCorrectMetadata()
    {
        var stringDownloader = new FakeWebDownloader();
        stringDownloader.FilesByUrl.Add("https://api.upcitemdb.com/prod/trial/lookup?upc=5060073306725", "./UpcItemDb/ngs2.json");

        var scraper = new UpcItemDbScraper();
        scraper.Initialize(new PlatformUtility("PS3", "sony_playstation3"), stringDownloader);

        var data = scraper.GetMetadataFromBarcode("5060073306725");

        Assert.Equal("Ninja Gaiden Sigma 2", data.Name);
        Assert.Equal(new MetadataSpecProperty("sony_playstation3"), data.Platforms.Single());
        Assert.Equal(string.Empty, data.Description);
    }

    [Fact]
    public void ScrapingNierReturnsCorrectMetadata()
    {
        var stringDownloader = new FakeWebDownloader();
        stringDownloader.FilesByUrl.Add("https://api.upcitemdb.com/prod/trial/lookup?upc=5021290041059", "./UpcItemDb/nier.json");

        var scraper = new UpcItemDbScraper();
        scraper.Initialize(new PlatformUtility("Xbox 360", "xbox360"), stringDownloader);

        var data = scraper.GetMetadataFromBarcode("5021290041059");

        Assert.Equal("Nier", data.Name);
        Assert.Equal(new MetadataSpecProperty("xbox360"), data.Platforms.Single());
        Assert.Equal(string.Empty, data.Description);
    }

    [Fact]
    public void ScrapingYakuzaDeadSoulsReturnsCorrectMetadata()
    {
        var stringDownloader = new FakeWebDownloader();
        stringDownloader.FilesByUrl.Add("https://api.upcitemdb.com/prod/trial/lookup?upc=5055277016662", "./UpcItemDb/yakuza_dead_souls.json");

        var scraper = new UpcItemDbScraper();
        scraper.Initialize(new PlatformUtility("PS3", "sony_playstation3"), stringDownloader);

        var data = scraper.GetMetadataFromBarcode("5055277016662");

        Assert.Equal("Yakuza Dead Souls Limited Edition Game", data.Name);
        Assert.Equal(new MetadataSpecProperty("sony_playstation3"), data.Platforms.Single());
        Assert.Equal("Yakuza Dead Souls Limited Edition Game PS3", data.Description);
    }

    [Fact]
    public void ScrapingTrackmaniaCorrectMetadata()
    {
        var stringDownloader = new FakeWebDownloader();
        stringDownloader.FilesByUrl.Add("https://api.upcitemdb.com/prod/trial/lookup?upc=3512289014953", "./UpcItemDb/tuf.json");

        var scraper = new UpcItemDbScraper();
        scraper.Initialize(new PlatformUtility("PC DVD", "pc_windows"), stringDownloader);

        var data = scraper.GetMetadataFromBarcode("3512289014953");

        Assert.Equal("Trackmania United Forever", data.Name);
        Assert.Equal(new MetadataSpecProperty("pc_windows"), data.Platforms.Single());
        Assert.Equal(string.Empty, data.Description);
        Assert.Equal("https://i2.onbuy.com/product/3b0506aef6364a5092dbfc6bd47d330e-l3294852.jpg", data.CoverImage.Path);
    }
}
