using SteamTagsImporter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SteamTagsImporter.Tests
{
    public class SteamTagScraperTests
    {
        private static SteamTagScraper Setup()
        {
            return new SteamTagScraper(id => new SteamTagScraper.Delistable<string>(File.ReadAllText("./hl2.html"), false));
        }

        [Fact]
        public void TagScrapingWorks()
        {
            var scraper = Setup();
            var tags = scraper.GetTags("220").Value.ToList();
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
            var scraper = new SteamTagScraper(id => new SteamTagScraper.Delistable<string>("<html></html>", true));
            var tags = scraper.GetTags("asdf");
            Assert.Empty(tags.Value);
        }
    }
}
