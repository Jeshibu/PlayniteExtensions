using System.IO;
using System.Linq;
using Xunit;

namespace SteamTagsImporter.Tests
{
    public class SteamTagScraperTests
    {
        private static SteamTagScraper SetupFile(string filePath, bool delisted = false)
        {
            var str = File.ReadAllText(filePath);
            return SetupString(str, delisted);
        }
        private static SteamTagScraper SetupString(string str, bool delisted = false)
        {
            return new SteamTagScraper((id, language) => new SteamTagScraper.Delistable<string>(str, false));
        }

        [Fact]
        public void TagScrapingWorks()
        {
            var scraper = SetupFile("./hl2.html");
            var tags = scraper.GetTags("220").Value.Select(t => t.Name).ToList();
            Assert.Contains("FPS", tags);
            Assert.Contains("Action", tags);
            Assert.Contains("Sci-fi", tags);
            Assert.Contains("Singleplayer", tags);
            Assert.Contains("Story Rich", tags);
            Assert.Contains("Shooter", tags);
            Assert.Contains("First-Person", tags);
            Assert.Contains("Adventure", tags);
            Assert.Contains("Dystopian", tags);
            Assert.Contains("Atmospheric", tags);
            Assert.Contains("Zombies", tags);
            Assert.Contains("Silent Protagonist", tags);
            Assert.Contains("Physics", tags);
            Assert.Contains("Aliens", tags);
            Assert.Contains("Great Soundtrack", tags);
            Assert.Contains("Horror", tags);
            Assert.Contains("Puzzle", tags);
            Assert.Contains("Multiplayer", tags);
            Assert.Contains("Moddable", tags);
            Assert.Equal(20, tags.Count);
        }

        [Fact]
        public void DelistedGameWillReturnEmptyTagList()
        {
            var scraper = SetupString("<html></html>", true);
            var tags = scraper.GetTags("asdf");
            Assert.Empty(tags.Value);
        }
    }
}
