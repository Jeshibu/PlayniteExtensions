using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyGamesLibrary
{
    public class AggregateMetadataGatherer : LibraryMetadataProvider
    {
        public AggregateMetadataGatherer(ILegacyGamesRegistryReader registryReader, IAppStateReader appStateReader)
        {
            RegistryReader = registryReader;
            AppStateReader = appStateReader;
        }

        public ILegacyGamesRegistryReader RegistryReader { get; }
        public IAppStateReader AppStateReader { get; }

        public IEnumerable<GameMetadata> GetGames(CancellationToken cancellationToken)
        {
            var installedGames = RegistryReader.GetGameData();
            var ownedGames = AppStateReader.GetUserOwnedGames();

            foreach (var game in ownedGames)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                var installation = installedGames.FirstOrDefault(g => g.InstallerUUID == game.InstallerUUID);
                var metadata = new GameMetadata
                {
                    GameId = game.InstallerUUID.ToString(),
                    Name = game.GameName,
                    CoverImage = new MetadataFile(game.GameCoverArt),
                    Description = game.GameDescription,
                    IsInstalled = installation != null,
                    Source = new MetadataNameProperty("Legacy Games"),

                    //the following are probably safe assumptions
                    Features = new HashSet<MetadataProperty> { new MetadataNameProperty("Single-player") },
                    Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") },
                };

                if (installation != null)
                {
                    metadata.InstallDirectory = installation.InstDir;
                    metadata.GameActions = new List<GameAction>
                    {
                        new GameAction
                        {
                            IsPlayAction = true,
                            Name = "Play",
                            Path = $@"{{InstallDir}}\{installation.GameExe}",
                            Type = GameActionType.File,
                        },
                    };
                    metadata.Icon = new MetadataFile($@"{installation.InstDir}\icon.ico");
                }

                yield return metadata;
            }
        }

        public override GameMetadata GetMetadata(Game game)
        {
            return GetGames(new CancellationToken()).FirstOrDefault(g => g.GameId == game.GameId);
        }
    }
}
