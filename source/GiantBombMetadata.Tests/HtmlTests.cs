using GiantBombMetadata.Api;
using GiantBombMetadata.SearchProviders;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace GiantBombMetadata.Tests;

public class HtmlTests
{
    [Theory]
    [InlineData("//")]
    [InlineData("///")]
    public void BunkPathTurnsIntoBaseUrl(string path)
    {
        string baseUrl = "https://www.giantbomb.com/soldier-of-fortune/3030-22547/";
        string htmlFormat = "<a href=\"{0}\">link</a>";
        string input = string.Format(htmlFormat, path);
        string expected = string.Format(htmlFormat, baseUrl);

        string processed = GiantBombHelper.MakeHtmlUrlsAbsolute(input, baseUrl);

        Assert.Equal(expected, processed);
    }

    [Fact]
    public void ThemeGamesParseCorrectly()
    {
        var apiClient = new FakeGiantBombApiClient();
        var platformUtility = new PlatformUtility();
        var downloader = new FakeWebDownloader(new Dictionary<string, string>() {
            { "https://www.giantbomb.com/games/?game_filter%5Btheme%5D=8", "html/theme-vietnam1.html" },
            { "https://www.giantbomb.com/games/?game_filter%5Btheme%5D=8&page=2", "html/theme-vietnam2.html" },
        });
        var searchProvider = new GiantBombGameThemeOrGenreSearchProvider(apiClient, new GiantBombScraper(downloader, platformUtility));

        var searchResult = searchProvider.Search("vietnam").ToList();

        Assert.Equal(80, searchResult.Count);

        var topSearchResult = searchProvider.ToGenericItemOption(searchResult.First()).Item;
        Assert.Equal("Vietnam", topSearchResult.Name);
        Assert.Equal(8, topSearchResult.Id);
        Assert.Equal("theme", topSearchResult.ResourceType);

        var detailsResult = searchProvider.GetDetails(topSearchResult).ToList();

        Assert.Equal(77, detailsResult.Count);
        Assert.Contains(detailsResult, i => i.Names.Single() == "Thud Ridge: American Aces in 'Nam");
    }

    private class FakeGiantBombApiClient : IGiantBombApiClient
    {
        public GiantBombGameDetails GetGameDetails(string gbGuid, CancellationToken cancellationToken) => throw new System.NotImplementedException();

        public GiantBombGamePropertyDetails GetGameProperty(string url, CancellationToken cancellationToken) => throw new System.NotImplementedException();

        private static GiantBombSearchResultItem[] DeserializeJson(string filePath)
        {
            var fileContents = File.ReadAllText(filePath);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<GiantBombResponse<GiantBombSearchResultItem[]>>(fileContents);
            return deserialized.Results;
        }

        public GiantBombSearchResultItem[] GetGenres(CancellationToken cancellationToken) => DeserializeJson("html/genres.json");

        public GiantBombSearchResultItem[] GetThemes(CancellationToken cancellationToken) => DeserializeJson("html/themes.json");


        public GiantBombSearchResultItem[] SearchGameProperties(string query, CancellationToken cancellationToken) => throw new System.NotImplementedException();

        public GiantBombSearchResultItem[] SearchGames(string query, CancellationToken cancellationToken) => throw new System.NotImplementedException();
    }
}
