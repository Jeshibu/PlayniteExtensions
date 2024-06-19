using System.Collections.Generic;
using PlayniteExtensions.Tests.Common;
using TvTropesMetadata.Scraping;
using Xunit;

namespace TvTropesMetadata.Tests
{
    public class WorkScraperTests
    {
        private FakeWebDownloader downloader = new FakeWebDownloader(new Dictionary<string, string>
        {
            { "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/KingdomHeartsII", "html/KingdomHeartsII.html" },
            { "https://tvtropes.org/pmwiki/pmwiki.php/KingdomHeartsII/TropesAToL", "html/KingdomHeartsII-TropesAToL.html" },
            { "https://tvtropes.org/pmwiki/pmwiki.php/KingdomHeartsII/TropesMToZ", "html/KingdomHeartsII-TropesMToZ.html" },
        });

        [Fact]
        public void KingdomHeartsIIScrapesCorrectly()
        {
            var scraper = new WorkScraper(downloader);
            var result = scraper.GetTropesForGame("https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/KingdomHeartsII");

            Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/KingdomHeartsII", downloader.CalledUrls);
            Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/KingdomHeartsII/TropesAToL", downloader.CalledUrls);
            Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/KingdomHeartsII/TropesMToZ", downloader.CalledUrls);

            Assert.Equal("Kingdom Hearts II", result.Title);
            Assert.Equal("https://static.tvtropes.org/pmwiki/pub/images/khii.png", result.CoverImageUrl);
            Assert.False(string.IsNullOrWhiteSpace(result.Description));

            Assert.Contains("Crouching Moron, Hidden Badass", result.Tropes);
            Assert.Contains("Pass Through the Rings", result.Tropes);
            Assert.DoesNotContain("Critical Existence Failure", result.Tropes);
            Assert.DoesNotContain("One-Steve Limit", result.Tropes);

            Assert.Contains("TRON", result.Franchises);
            Assert.Contains("The Nightmare Before Christmas", result.Franchises);
            Assert.Contains("The Little Mermaid", result.Franchises);
            Assert.Contains("Lilo & Stitch", result.Franchises);
            Assert.Contains("Kingdom Hearts", result.Franchises);
            Assert.Contains("The Lion King", result.Franchises);
            Assert.Contains("Hercules", result.Franchises);
        }
    }
}
