using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Barnite.Tests.Bol
{
    public class BolScraperTests
    {
        [Fact]
        public void ScrapingCodBlopsReturnsCorrectMetadata()
        {
            var stringDownloader = new FakeWebDownloader();
            stringDownloader.FilesByUrl.Add("https://www.bol.com/nl/nl/s/?searchtext=5030917291821", "./Bol/codblopscw_search.html");
            stringDownloader.FilesByUrl.Add("https://www.bol.com/nl/nl/p/call-of-duty-black-ops-cold-war/9300000018256305/", "./Bol/codblopscw.html");

            var scraper = new BolScraper();
            var platformDefinitions = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "PS4", new[]{ "sony_playstation4" } },
                { "Playstation 4", new[]{ "sony_playstation4" } },
                { "Playstation 5", new[]{ "sony_playstation5" } },
            };
            scraper.Initialize(new PlatformUtility(platformDefinitions), stringDownloader);

            var data = scraper.GetMetadataFromBarcode("5030917291821");

            Assert.Equal("Call of Duty Black Ops: Cold War", data.Name);
            Assert.Contains(new MetadataNameProperty("Activision"), data.Publishers);
            Assert.Contains(new MetadataSpecProperty("sony_playstation4"), data.Platforms);
            Assert.Contains(new MetadataSpecProperty("sony_playstation5"), data.Platforms);
            Assert.Contains(new MetadataNameProperty("Action"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Adventure"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Role Playing Game (RPG)"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Shooter"), data.Genres);
            Assert.Contains(new MetadataNameProperty("PEGI 18"), data.AgeRatings);
            Assert.Equal(new ReleaseDate(2020, 11, 13), data.ReleaseDate);
            Assert.Contains(new MetadataNameProperty("PAL"), data.Regions);
            Assert.Equal("https://media.s-bol.com/JPmDVYRQDEPD/550x694.jpg", data.CoverImage.Path);
        }

        [Fact]
        public void ScrapingPsyvariarReturnsCorrectMetadata()
        {
            var stringDownloader = new FakeWebDownloader();
            stringDownloader.FilesByUrl.Add("https://www.bol.com/nl/nl/s/?searchtext=5017783459876", "./Bol/psyvariar_search.html");
            stringDownloader.FilesByUrl.Add("https://www.bol.com/nl/nl/p/psyvariar-ps2/1004004000009690/", "./Bol/psyvariar.html");

            var scraper = new BolScraper();
            var platformDefinitions = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "PS2", new[]{ "sony_playstation2" } },
                { "Playstation 2", new[]{ "sony_playstation2" } },
            };
            scraper.Initialize(new PlatformUtility(platformDefinitions), stringDownloader);

            var data = scraper.GetMetadataFromBarcode("5017783459876");

            Assert.Equal("Psyvariar", data.Name);
            Assert.Contains(new MetadataNameProperty("Empire"), data.Publishers);
            Assert.Contains(new MetadataSpecProperty("sony_playstation2"), data.Platforms);
            Assert.Contains(new MetadataNameProperty("Shooter"), data.Genres);
            Assert.Contains(new MetadataNameProperty("PEGI 12"), data.AgeRatings);
            Assert.Contains(new MetadataNameProperty("PAL"), data.Regions);
            Assert.Equal("https://media.s-bol.com/xkk47PVxXg2l/550x785.jpg", data.CoverImage.Path);
        }
    }
}
