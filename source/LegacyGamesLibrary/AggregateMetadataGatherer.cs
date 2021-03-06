using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyGamesLibrary
{
    public class AggregateMetadataGatherer : LibraryMetadataProvider
    {
        public AggregateMetadataGatherer(ILegacyGamesRegistryReader registryReader, IAppStateReader appStateReader, IPlayniteAPI playniteAPI)
        {
            RegistryReader = registryReader;
            AppStateReader = appStateReader;
            PlayniteAPI = playniteAPI;
        }

        public ILegacyGamesRegistryReader RegistryReader { get; }
        public IAppStateReader AppStateReader { get; }
        private IPlayniteAPI PlayniteAPI { get; }
        private ILogger logger = LogManager.GetLogger();

        public IEnumerable<GameMetadata> GetGames(CancellationToken cancellationToken)
        {
            try
            {
                var installedGames = RegistryReader.GetGameData();
                var ownedGames = AppStateReader.GetUserOwnedGames();

                var output = new List<GameMetadata>();

                if (ownedGames == null)
                {
                    PlayniteAPI.Notifications.Add(new NotificationMessage("legacy-games-error", $"No Legacy Games games found - check your Legacy Games client installation. Click this message to download it.", NotificationType.Error, () =>
                    {
                        try
                        {
                            Process.Start("https://legacygames.com/gameslauncher/");
                        }
                        catch { }
                    }));
                    return output;
                }

                foreach (var game in ownedGames)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return output;

                    var installation = installedGames?.FirstOrDefault(g => g.InstallerUUID == game.InstallerUUID);
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
                        metadata.Icon = new MetadataFile($@"{installation.InstDir}\icon.ico");
                    }

                    output.Add(metadata);
                }
                return output;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to gather metadata");
                PlayniteAPI.Notifications.Add(new NotificationMessage("legacy-games-error", $"Failed to get Legacy Games games: {ex.Message}", NotificationType.Error));
                return new GameMetadata[0];
            }
        }

        public override GameMetadata GetMetadata(Game game)
        {
            return GetGames(new CancellationToken()).FirstOrDefault(g => g.GameId == game.GameId);
        }
    }
}
