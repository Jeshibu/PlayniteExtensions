using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Reflection;

namespace BigFishLibrary
{
    public class BigFishLibrary : LibraryPlugin
    {
        private static readonly string iconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.png");

        private static readonly ILogger logger = LogManager.GetLogger();

        private BigFishLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("37995df7-2ce2-4f7c-83a3-618138ae745d");

        public override string Name => "Big Fish Games";

        public override LibraryClient Client => new BigFishLibraryClient(RegistryReader, iconPath);

        public override string LibraryIcon => iconPath;

        private BigFishRegistryReader RegistryReader { get; }
        private BigFishMetadataProvider MetadataProvider { get; }

        public BigFishLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new BigFishLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = false
            };
            RegistryReader = new BigFishRegistryReader(new RegistryValueProvider());
            MetadataProvider = new BigFishMetadataProvider(RegistryReader);
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            try
            {
                foreach (var game in PlayniteApi.Database.Games.Where(g => g.PluginId == Id && g.IsInstalled))
                {
                    if (string.IsNullOrEmpty(game.InstallDirectory))
                    {
                        game.IsInstalled = false;
                        PlayniteApi.Database.Games.Update(game);
                        continue;
                    }

                    var dir = new DirectoryInfo(game.InstallDirectory);
                    if (!dir.Exists || !dir.EnumerateFileSystemInfos().Any())
                    {
                        game.IsInstalled = false;
                        PlayniteApi.Database.Games.Update(game);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error setting uninstalled status");
            }

            var registryReader = new BigFishRegistryReader(new RegistryValueProvider());
            var gameIds = registryReader.GetInstalledGameIds();
            foreach (var gameId in gameIds)
            {
                if (gameId == "F7315T1L1") //Big Fish Casino, not visible in the client and apparently broken
                    continue;

                var game = registryReader.GetGameDetails(gameId);
                yield return MetadataProvider.GetMetadata(game.Sku, minimal: true);
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new BigFishLibrarySettingsView();
        }

        public override LibraryMetadataProvider GetMetadataDownloader() => MetadataProvider;

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            var installData = RegistryReader.GetGameDetails(args.Game.GameId);

            if (installData == null)
            {
                logger.Debug($"No install data found for {args.Game.Name}, ID: {args.Game.GameId}");
                PlayniteApi.Dialogs.ShowErrorMessage("No install data found.", "Big Fish Games launch error");
            }

            var directory = new FileInfo(installData.ExecutablePath).Directory;
            var files = directory.GetFiles("*.exe")
                .Where(f =>
                    !f.Name.Equals("uninstall.exe", StringComparison.InvariantCultureIgnoreCase)
                    && !f.FullName.Equals(installData.ExecutablePath, StringComparison.InvariantCultureIgnoreCase)
                ).ToArray();

            if (files.Length != 1)
                yield break;

            yield return new AutomaticPlayController(args.Game)
            {
                Path = files.Single().FullName,
                TrackingMode = TrackingMode.Default
            };
        }

        public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            var installData = RegistryReader.GetGameDetails(args.Game.GameId);

            if (installData == null)
            {
                logger.Debug($"No install data found for {args.Game.Name}, ID: {args.Game.GameId}");
                PlayniteApi.Dialogs.ShowErrorMessage("No install data found.", "Big Fish Games launch error");
            }

            var directory = new FileInfo(installData.ExecutablePath).Directory;
            var files = directory.GetFiles("*.exe")
                .Where(f => f.Name.Equals("uninstall.exe", StringComparison.InvariantCultureIgnoreCase)).ToArray();

            if (files.Length != 1)
            {
                yield break;
            }

            yield return new BigFishUninstallController(args.Game, RegistryReader, files[0].FullName);
        }
    }
}