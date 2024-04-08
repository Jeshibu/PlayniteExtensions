using BigFishMetadata;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace BigFishLibrary
{
    public class BigFishMetadataProvider : LibraryMetadataProvider
    {
        private readonly BigFishRegistryReader registryReader;
        private readonly IGameSearchProvider<BigFishSearchResultGame> gameSearchProvider;
        private readonly BigFishLibrarySettings settings;
        private readonly BigFishOnlineLibraryScraper scraper;

        public BigFishMetadataProvider(BigFishRegistryReader registryReader, IGameSearchProvider<BigFishSearchResultGame> gameSearchProvider, BigFishLibrarySettings settings, BigFishOnlineLibraryScraper scraper)
        {
            this.registryReader = registryReader;
            this.gameSearchProvider = gameSearchProvider;
            this.settings = settings;
            this.scraper = scraper;
        }

        public override GameMetadata GetMetadata(Game game)
        {
            var onlineData = GetOnlineMetadata(game);
            var offlineData = GetOfflineMetadata(game);
            return Merge(offlineData, onlineData);
        }

        public GameMetadata GetOfflineMetadata(Game game)
        {
            return GetOfflineMetadata(game.GameId, false);
        }

        public GameMetadata GetOfflineMetadata(string sku, bool minimal)
        {
            var registryDetails = registryReader.GetGameDetails(sku);
            var output = new GameMetadata
            {
                GameId = registryDetails.Sku,
                Name = registryDetails.Name,
                Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") },
                InstallDirectory = new FileInfo(registryDetails.ExecutablePath).DirectoryName,
                IsInstalled = true,
                Source = new MetadataNameProperty("Big Fish Games"),
            };
            if (!minimal)
            {
                if (File.Exists(registryDetails.Thumbnail))
                    output.Icon = new MetadataFile(registryDetails.Thumbnail);

                string id = new string(registryDetails.Sku.SkipWhile(char.IsLetter).TakeWhile(char.IsNumber).ToArray());
                output.Links = new List<Link> { new Link("Big Fish Store Page", $"https://www.bigfishgames.com/games/{id}/") };
            }
            return output;
        }

        public GameMetadata GetOnlineMetadata(Game game)
        {
            if (gameSearchProvider.TryGetDetails(game, out var gameDetails, CancellationToken.None))
                return gameDetails.ToMetadata();
            return null;
        }

        public IEnumerable<GameMetadata> GetGames()
        {
            var games = GetOnlineGames().ToDictionary(g => g.GameId);
            foreach (var offlineGame in GetOfflineGames())
            {
                if (games.TryGetValue(offlineGame.GameId, out var onlineGame))
                    games[offlineGame.GameId] = Merge(offlineGame, onlineGame);
                else
                    games[offlineGame.GameId] = offlineGame;
            }
            return games.Values;
        }

        private static GameMetadata Merge(GameMetadata offlineData, GameMetadata onlineData)
        {
            if (offlineData == null) return onlineData;
            if (onlineData == null) return offlineData;

            onlineData.Platforms = offlineData.Platforms;
            onlineData.InstallDirectory = offlineData.InstallDirectory;
            onlineData.IsInstalled = offlineData.IsInstalled;
            onlineData.Icon = offlineData.Icon;
            return onlineData;
        }

        private IEnumerable<GameMetadata> GetOfflineGames()
        {
            var gameIds = registryReader.GetInstalledGameIds();
            foreach (var gameId in gameIds)
            {
                if (gameId == "F7315T1L1") //Big Fish Casino, not visible in the client and apparently broken
                    continue;

                var game = registryReader.GetGameDetails(gameId);
                yield return GetOfflineMetadata(game.Sku, minimal: true);
            }
        }

        private IEnumerable<GameMetadata> GetOnlineGames()
        {
            if (!settings.ImportFromOnline)
                return Enumerable.Empty<GameMetadata>();

            var products = scraper.GetGames();

            return products.Select(p => new GameMetadata
            {
                GameId = p.Sku,
                Name = p.Name,
                Source = new MetadataNameProperty("Big Fish Games"),
            });
        }
    }
}