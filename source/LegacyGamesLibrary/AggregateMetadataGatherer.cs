using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using PlayniteExtensions.Common;

namespace LegacyGamesLibrary
{
    public class AggregateMetadataGatherer : LibraryMetadataProvider
    {
        public AggregateMetadataGatherer(ILegacyGamesRegistryReader registryReader, IAppStateReader appStateReader, IPlayniteAPI playniteAPI, LegacyGamesLibrarySettings settings)
        {
            RegistryReader = registryReader;
            AppStateReader = appStateReader;
            PlayniteAPI = playniteAPI;
            Settings = settings;
        }

        public ILegacyGamesRegistryReader RegistryReader { get; }
        public IAppStateReader AppStateReader { get; }
        private IPlayniteAPI PlayniteAPI { get; }
        private ILogger logger = LogManager.GetLogger();
        private LegacyGamesLibrarySettings Settings { get; }

        public IEnumerable<GameMetadata> GetGames(CancellationToken cancellationToken)
        {
            try
            {
                var installedGames = RegistryReader.GetGameData();
                var ownedGames = AppStateReader.GetUserOwnedGames();

                var output = new List<GameMetadata>();

                if (ownedGames == null)
                {
                    PlayniteAPI.Notifications.Add(new NotificationMessage("legacy-games-error", $"No Legacy Games games found - check your Legacy Games client installation. Click this message to download it. If it's already installed, please open the list of non-installed games there, or install one game.", NotificationType.Error, () =>
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

                    var installation = installedGames?.FirstOrDefault(g => Guid.TryParse(game.InstallerUUID, out Guid installerId) && g.InstallerUUID == installerId);
                    var metadata = new GameMetadata
                    {
                        GameId = game.InstallerUUID.ToString(),
                        Name = game.GameName,
                        Description = game.GameDescription,
                        InstallSize = game.GameInstalledSize.ParseInstallSize(CultureInfo.InvariantCulture),
                        IsInstalled = installation != null,
                        Source = new MetadataNameProperty("Legacy Games"),

                        //the following are probably safe assumptions
                        Features = new HashSet<MetadataProperty> { new MetadataNameProperty("Single-player") },
                        Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") },
                    };

                    if (Settings.UseCovers)
                        metadata.CoverImage = new MetadataFile(game.GameCoverArt);

                    if (Settings.NormalizeGameNames && metadata.Name.EndsWith(" CE"))
                        metadata.Name = metadata.Name.Remove(metadata.Name.Length - 3) + " Collector's Edition";

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
