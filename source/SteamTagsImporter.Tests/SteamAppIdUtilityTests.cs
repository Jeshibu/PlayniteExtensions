using Playnite.SDK.Models;
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
    public class SteamAppIdUtilityTests
    {
        private class FakeWebClient : IWebClient
        {
            public FakeWebClient(string localFilePath)
            {
                LocalFilePath = localFilePath;
            }

            public string LocalFilePath { get; }

            public void Dispose()
            {
            }

            public void DownloadFile(string address, string fileName)
            {
                File.Copy(LocalFilePath, fileName, true);
            }
        }

        private static SteamAppIdUtility Setup()
        {
            var scraper = new SteamAppIdUtility(() => new FakeWebClient("./applist.json"));
            return scraper;
        }

        [Fact]
        public void NullLinkCollectionDoesNotThrowException()
        {
            var game = new Game("THOR.N");
            var util = Setup();
            var id = util.GetSteamGameId(game);
        }

        [Theory]
        [InlineData("Half-Life 2", "220")]
        [InlineData("HalfLife 2", "220")]
        public void GamesCanBeFoundByName(string name, string expectedId)
        {
            var game = new Game(name);
            var util = Setup();
            var id = util.GetSteamGameId(game);
            Assert.Equal(expectedId, id);
        }
    }
}
