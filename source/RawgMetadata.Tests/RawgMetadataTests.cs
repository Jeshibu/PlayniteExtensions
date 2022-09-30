using PlayniteExtensions.Tests.Common;
using Rawg.Common;
using RawgMetadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Rawg.Tests
{
    public class RawgMetadataTests
    {
        private static string Key { get; } = "asdf";

        [Fact]
        public void CanFetchSomething()
        {
            string slug = "doom";
            RawgApiClient client = new RawgApiClient(new FakeWebDownloader($"https://api.rawg.io/api/games/{slug}?key={Key}", "./doom.json"), Key);

            var game = client.GetGame(slug);

            Assert.Equal("DOOM (2016)", game.Name);
        }
    }
}
