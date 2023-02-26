using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
