using PlayniteExtensions.Tests.Common;
using SteamTagsImporter.BulkImport;
using System.Linq;
using Xunit;

namespace SteamTagsImporter.Tests
{
    public class SteamPropertySearchProviderTests
    {
        private FakeWebDownloader downloader = new FakeWebDownloader("https://store.steampowered.com/search/?category2=35&l=english", "search.html");
        private SteamTagsImporterSettings settings = new SteamTagsImporterSettings { LanguageKey = "english" };
        private SteamSearch steamSearch;
        private SteamPropertySearchProvider searchProvider;

        public SteamPropertySearchProviderTests()
        {
            steamSearch = new SteamSearch(downloader, settings);
            downloader.FilesByUrl.Add(steamSearch.GetSearchRequestUrl("tags", "1254552", 000), "football000.json");
            downloader.FilesByUrl.Add(steamSearch.GetSearchRequestUrl("tags", "1254552", 050), "football050.json");
            downloader.FilesByUrl.Add(steamSearch.GetSearchRequestUrl("tags", "1254552", 100), "football100.json");
            searchProvider = new SteamPropertySearchProvider(steamSearch);
        }

        [Theory]
        [InlineData("22 Jun, 2024", 2024, 6, 22)]
        [InlineData("August 2024", 2024, 8)]
        [InlineData("2025", 2025)]
        [InlineData("Q4 2024", 2024, 12)]
        [InlineData("Coming soon", null)]
        public void DateParseTests(string input, int? expectedYear = null, int? expectedMonth = null, int? expectedDay = null)
        {
            var releaseDate = SteamSearch.ParseReleaseDate(input);

            Assert.Equal(expectedYear, releaseDate?.Year);
            Assert.Equal(expectedMonth, releaseDate?.Month);
            Assert.Equal(expectedDay, releaseDate?.Day);

            if (expectedYear == null)
                Assert.Null(releaseDate);
        }

        [Fact]
        public void FootballHasResults()
        {
            var props = searchProvider.Search("Football (American)").ToList();
            Assert.Single(props);

            var footballSearchResults = searchProvider.GetDetails(props[0]).ToList();
            Assert.Equal(116, footballSearchResults.Count);

            var names = footballSearchResults.Select(x => x.Names.Single());
            Assert.Contains("Madden NFL 24", names);
            Assert.Contains("Greats of the Gridiron", names);
        }
    }
}
