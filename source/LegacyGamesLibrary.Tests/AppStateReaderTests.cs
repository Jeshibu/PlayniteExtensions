using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LegacyGamesLibrary.Tests
{
    public class AppStateReaderTests
    {
        [Fact]
        public void GetUserOwnedGamesReturnsCorrectAmountOfGames()
        {
            AppStateReader reader = new AppStateReader("./app-state-trimmed-2games.json");
            var games = reader.GetUserOwnedGames().ToList();
            Assert.Equal(2, games.Count);
        }

        [Fact]
        public void GetUserOwnedGamesDeduplicates()
        {
            //there's overlap in bundles in this file's owned games
            AppStateReader reader = new AppStateReader("./app-state-trimmed-11games.json");
            var games = reader.GetUserOwnedGames().ToList();
            Assert.Equal(11, games.Count);
        }

        [Fact]
        public void GetUserOwnedGamesReturnsNullIfFileIsMissing()
        {
            AppStateReader reader = new AppStateReader("./missing-file.json");
            var games = reader.GetUserOwnedGames();
            Assert.Null(games);
        }
    }
}
