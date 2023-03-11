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
using Xunit;

namespace XboxMetadata.Tests
{
    public class XboxMetadataProviderTests
    {
        private XboxMetadataProvider Setup()
        {
            var options = new MetadataRequestOptions(new Game("Sniper Elite 5"), backgroundDownload: true);

            var playniteSettingsApi = new Mock<IPlayniteSettingsAPI>(MockBehavior.Strict);
            playniteSettingsApi.SetupGet(a => a.AgeRatingOrgPriority).Returns(AgeRatingOrg.ESRB);

            var database = new Mock<IGameDatabaseAPI>(MockBehavior.Strict);
            database.SetupGet(d => d.Platforms).Returns(new FakeItemCollection<Platform>());

            var playniteApi = new Mock<IPlayniteAPI>(MockBehavior.Strict);
            playniteApi.SetupGet(a => a.ApplicationSettings).Returns(playniteSettingsApi.Object);

            string market = "en-us";
            string query = Uri.EscapeDataString(options.GameData.Name);

            var downloader = new FakeWebDownloader(new Dictionary<string, string>
            {
                { $"https://www.microsoft.com/msstoreapiprod/api/autosuggest?market={market}&sources=DCatAll-Products&filter=+ClientType:StoreWeb&counts=5&query={query}", "sniper elite 5 search.json" },
                { $"https://www.xbox.com/{market}/games/store/-/9pp8q82h79lc", "sniper elite 5 details.html" }
            });

            var scraper = new XboxScraper(downloader);

            return new XboxMetadataProvider(options, new XboxMetadataSettings(), playniteApi.Object, scraper, new PlatformUtility("xbox", "xbox"));
        }

        [Fact]
        public void SearchOnlyIncludesGames()
        {
            string market = "en-us";
            string query = "Sniper Elite 5";
            string escapedQuery = Uri.EscapeDataString(query);

            var downloader = new FakeWebDownloader($"https://www.microsoft.com/msstoreapiprod/api/autosuggest?market={market}&sources=DCatAll-Products&filter=+ClientType:StoreWeb&counts=5&query={escapedQuery}", "sniper elite 5 search.json");
            //https://www.microsoft.com/msstoreapiprod/api/autosuggest?market=en-us&sources=DCatAll-Products&filter=+ClientType:StoreWeb&counts=5&query=Sniper%2520Elite%25205
            var scraper = new XboxScraper(downloader);
            var result = scraper.Search(query).ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void FetchesSniperElite5()
        {
            var metadataProvider = Setup();
            var dev = metadataProvider.GetDevelopers(new GetMetadataFieldArgs());
            Assert.Single(dev, new MetadataNameProperty("Rebellion"));
        }
    }
}
