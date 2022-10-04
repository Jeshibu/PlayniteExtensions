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
        public void GetUserOwnedGamesReturnsNullIfFileIsMissing()
        {
            AppStateReader reader = new AppStateReader("./missing-file.json");
            var games = reader.GetUserOwnedGames();
            Assert.Null(games);
        }

        [Fact]
        public void Old_GetUserOwnedGamesReturnsCorrectAmountOfGames()
        {
            AppStateReader reader = new AppStateReader("./app-state-trimmed-2games.json");
            var games = reader.GetUserOwnedGames().ToList();
            Assert.Equal(2, games.Count);
        }

        [Fact]
        public void Old_GetUserOwnedGamesDeduplicates()
        {
            //there's overlap in bundles in this file's owned games
            AppStateReader reader = new AppStateReader("./app-state-trimmed-11games.json");
            var games = reader.GetUserOwnedGames().ToList();
            Assert.Equal(11, games.Count);
        }

        [Fact]
        public void Old_GetUserOwnedGamesReturnsNullIfMissingCatalogSection()
        {
            AppStateReader reader = new AppStateReader("./app-state-broken-no-catalog.json");
            var games = reader.GetUserOwnedGames();
            Assert.Null(games);
        }

        [Fact]
        public void Old_GetUserOwnedGamesReturnsNullIfMissingDownloadsSection()
        {
            AppStateReader reader = new AppStateReader("./app-state-broken-no-downloads.json");
            var games = reader.GetUserOwnedGames();
            Assert.Null(games);
        }

        [Fact]
        public void Old_GetUserOwnedGamesDoesNotThrowIfBundleIsMissingGames()
        {
            AppStateReader reader = new AppStateReader("./app-state-bundle-missing-games.json");
            var games = reader.GetUserOwnedGames();
            Assert.Empty(games);
        }


        [Fact]
        public void New_GetUserOwnedGamesReturnsCorrectAmountOfGames()
        {
            AppStateReader reader = new AppStateReader("./app-state-trimmed-2games-new.json");
            var games = reader.GetUserOwnedGames().ToList();
            Assert.Equal(2, games.Count);
        }

        [Fact]
        public void New_GetUserOwnedGamesDeduplicates()
        {
            //there's overlap in bundles in this file's owned games
            AppStateReader reader = new AppStateReader("./app-state-trimmed-11games-new.json");
            var games = reader.GetUserOwnedGames().ToList();
            Assert.Equal(11, games.Count);
        }

        [Fact]
        public void New_GetUserOwnedGamesReturnsNullIfMissingCatalogSection()
        {
            AppStateReader reader = new AppStateReader("./app-state-broken-no-catalog-new.json");
            var games = reader.GetUserOwnedGames();
            Assert.Null(games);
        }

        [Fact]
        public void New_GetUserOwnedGamesReturnsNullIfMissingDownloadsSection()
        {
            AppStateReader reader = new AppStateReader("./app-state-broken-no-downloads-new.json");
            var games = reader.GetUserOwnedGames();
            Assert.Null(games);
        }

        [Fact]
        public void New_GetUserOwnedGamesDoesNotThrowIfBundleIsMissingGames()
        {
            AppStateReader reader = new AppStateReader("./app-state-bundle-missing-games-new.json");
            var games = reader.GetUserOwnedGames();
            Assert.Empty(games);
        }

        [Fact]
        public void GetUserOwnedGamesReturnsGamesThatAreOnlyInTheGiveawaySection()
        {
            AppStateReader reader = new AppStateReader("./app-state-giveaway-only.json");
            var games = reader.GetUserOwnedGames().ToList();
            Assert.Single(games);
        }
    }
}
