using MobyGamesMetadata.Api;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.IO;
using System.Linq;
using Xunit;

namespace MobyGamesMetadata.Tests;

public class SearchResultTests
{
    [Fact]
    public void NumericTitleUrlParsesCorrectId()
    {
        var sr = new SearchResult();
        sr.SetUrlAndId("https://www.mobygames.com/game/86550/640/");
        Assert.Equal(86550, sr.Id);
    }

    [Fact]
    public void SearchResultParsingTest()
    {
        var webViewFactory = new FakeWebViewFactory(new()
        {
            { "https://www.mobygames.com/search/?q=Phantom%20Breaker%20Omnia&type=game&adult=true", "html/search-phantom-breaker-omnia.html" }
        });
        MobyGamesScraper scraper = new(new PlatformUtility([]), webViewFactory);
        var searchResults = scraper.GetGameSearchResults("Phantom Breaker Omnia").ToList();
        Assert.NotEmpty(searchResults);
    }
}