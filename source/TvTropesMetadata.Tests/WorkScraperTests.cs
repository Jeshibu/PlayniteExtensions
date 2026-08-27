using System.Collections.Generic;
using PlayniteExtensions.Tests.Common;
using TvTropesMetadata.Scraping;
using Xunit;
using System.Linq;
using TvTropesMetadata.SearchProviders;

namespace TvTropesMetadata.Tests;

public class WorkScraperTests
{
    private readonly FakeWebViewFactory fakeWebViewFactory = new(new Dictionary<string, string>
    {
        { "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/KingdomHeartsII", "html/KingdomHeartsII.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/KingdomHeartsII/TropesAToL", "html/KingdomHeartsII-TropesAToL.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/KingdomHeartsII/TropesMToZ", "html/KingdomHeartsII-TropesMToZ.html" },
        { BaseScraper.GetDuckDuckGoSearchUrl("hellblade"), "html/ddg-hellblade.html" },
    });

    [Fact]
    public void KingdomHeartsIIScrapesCorrectly()
    {
        var scraper = new WorkScraper(fakeWebViewFactory);
        var result = scraper.GetTropesForGame("https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/KingdomHeartsII");

        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/KingdomHeartsII", fakeWebViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/KingdomHeartsII/TropesAToL", fakeWebViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/KingdomHeartsII/TropesMToZ", fakeWebViewFactory.CalledUrls);

        Assert.Equal("Kingdom Hearts II", result.Title);
        Assert.Single(result.CoverImageUrls, "https://mediaproxy.tvtropes.org/width/1200/https://static.tvtropes.org/pmwiki/pub/images/kh2_heart.png");
        Assert.False(string.IsNullOrWhiteSpace(result.Description));

        Assert.Contains("Crouching Moron, Hidden Badass", result.Tropes);
        Assert.Contains("Pass Through the Rings", result.Tropes);
        Assert.DoesNotContain("Critical Existence Failure", result.Tropes);
        Assert.DoesNotContain("One-Steve Limit", result.Tropes);

        Assert.Contains("TRON", result.Franchises);
        Assert.Contains("The Lion King", result.Franchises);
        Assert.Contains("The Little Mermaid", result.Franchises);
        Assert.Contains("Kingdom Hearts", result.Franchises);
        Assert.Contains("Hercules (Disney)", result.Franchises);
    }

    [Fact]
    public void SearchProducesResults()
    {
        var scraper = new WorkScraper(fakeWebViewFactory);
        var sp = new WorkSearchProvider(scraper, new());

        var searchResults = sp.Search("hellblade").ToList();

        Assert.Equal(2, searchResults.Count);

        var result = searchResults[0];
        Assert.Equal("Hellblade: Senua's Sacrifice", result.Name);
        Assert.Equal("https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/HellbladeSenuasSacrifice", result.Url);
        Assert.Contains("Hellblade: Senua's Sacrifice is an Action-Adventure Video Game developed and — for the first time — published by Ninja Theory. Based upon Celtic and Norse Mythology, the game tells the story of the eponymous Senua, a warrior", result.Description);
    }
}
