using System.IO;
using System.Linq;
using Xunit;

namespace LaunchBoxMetadata.Tests
{
    public class DatabaseTests
    {
        public static LaunchBoxDatabase Setup()
        {
            var dir = Path.GetTempPath();
            var db = new LaunchBoxDatabase(dir);
            db.CreateDatabase(new LaunchBoxXmlParser(@"Metadata.xml"));
            return db;
        }

        [Fact]
        public void ReturnsSearchResults()
        {
            var db = Setup();
            var searchResult = db.SearchGames("alien", 50).ToList();
            Assert.Equal(6, searchResult.Count);
        }

        [Fact]
        public void DeduplicatesNames()
        {
            var db = Setup();
            var searchResult = db.SearchGames("lylat wars", 50).ToList();
            Assert.Single(searchResult);
        }
    }
}
