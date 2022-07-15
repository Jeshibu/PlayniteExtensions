using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GameJoltLibrary.Tests
{
    public class WttfReaderTests
    {
        private WttfReader Setup()
        {
            return new WttfReader("./packages.wttf", "./games.wttf");
        }

        [Fact]
        public void GetGamesReturnsAllGames()
        {
            var reader = Setup();
            var games = reader.GetGames();
            Assert.Equal(4, games.Count);
        }

        [Fact]
        public void GetPackagesReturnsAllPackages()
        {
            var reader = Setup();
            var packages = reader.GetPackages();
            Assert.Equal(4, packages.Count);
        }
    }
}
