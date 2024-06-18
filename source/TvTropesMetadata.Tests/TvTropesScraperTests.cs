using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;
using System.Linq;
using TvTropesMetadata.Scraping;
using Xunit;

namespace TvTropesMetadata.Tests
{
    public class TropeScraperTests
    {
        private FakeWebDownloader downloader = new FakeWebDownloader(new Dictionary<string, string>
        {
            { "https://tvtropes.org/pmwiki/pmwiki.php/Main/TheAtoner", "html/TheAtoner.html" },
            { "https://tvtropes.org/pmwiki/pmwiki.php/TheAtoner/VideoGames", "html/TheAtoner-VideoGames.html" },
            { "https://tvtropes.org/pmwiki/pmwiki.php/Main/StalkerWithACrush", "html/StalkerWithACrush.html" },
            { "https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VideoGames", "html/StalkerWithACrush-VideoGames.html" },
            { "https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VisualNovels", "html/StalkerWithACrush-VisualNovels.html" },
        });

        [Fact]
        public void SubcategoryLinksParse()
        {
            var scraper = new TropeScraper(downloader);
            var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/StalkerWithACrush");

            Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VideoGames", downloader.CalledUrls);
            Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VisualNovels", downloader.CalledUrls);

            Assert.NotNull(result);
            Assert.Equal("Stalker with a Crush", result.Title);
            Assert.NotEmpty(result.Items);

            ContainsGame(result, "Alice: Madness Returns", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/AliceMadnessReturns");
            ContainsGame(result, "Doki Doki Literature Club!", "https://tvtropes.org/pmwiki/pmwiki.php/VisualNovel/DokiDokiLiteratureClub");
        }

        [Fact]
        public void MixedSubcategoryAndFolderLinksParse()
        {
            var scraper = new TropeScraper(downloader);
            var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/TheAtoner");

            Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/TheAtoner/VideoGames", downloader.CalledUrls);

            Assert.NotNull(result);
            Assert.Equal("The Atoner", result.Title);
            Assert.NotEmpty(result.Items);

            ContainsGame(result, "Double Homework", "https://tvtropes.org/pmwiki/pmwiki.php/VisualNovel/DoubleHomework");
            ContainsGame(result, "Bendy and the Ink Machine", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/BendyAndTheInkMachine");
        }

        private void ContainsGame(ParsedTropePage tropePage, string title, string url)
        {
            var work = tropePage.Items.SelectMany(i=>i.Works).SingleOrDefault(w=>w.Title == title);
            Assert.NotNull(work);
            Assert.Contains(url, work.Urls);
        }
    }
}
