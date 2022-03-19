using SteamTagsImporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SteamTagsImporterTests
{
    public class SteamTagScraperTests
    {
        [Fact]
        public void TagScrapingWorks()
        {
            var scraper = new SteamTagScraper();
            var tags = scraper.GetTags("1426210");
            Assert.NotEmpty(tags);
        }
    }
}
