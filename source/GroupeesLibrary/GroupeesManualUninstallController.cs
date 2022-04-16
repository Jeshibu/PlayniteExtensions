using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections.Generic;
using System.IO;

namespace GroupeesLibrary
{
    public class GroupeesManualUninstallController : UninstallController
    {
        public GroupeesManualUninstallController(Game game, GroupeesLibrarySettings settings, IPlayniteAPI playniteApi, Plugin plugin) : base(game)
        {
            Settings = settings;
            PlayniteApi = playniteApi;
            Plugin = plugin;
        }

        public GroupeesLibrarySettings Settings { get; }
        public IPlayniteAPI PlayniteApi { get; }
        public Plugin Plugin { get; }
        private ILogger logger = LogManager.GetLogger();

        public override void Uninstall(UninstallActionArgs args)
        {
            if (!Settings.InstallData.TryGetValue(Game.GameId, out var installData))
            {
                logger.Debug($"No install data found for {Game.Name}, ID: {Game.GameId}");
                var openPurchasesOption = new MessageBoxOption("Open purchases page");
                var result = PlayniteApi.Dialogs.ShowMessage(
                    "No install/download data found for this game. Please re-run the game import after checking your purchases page.",
                    "Install data missing",
                    System.Windows.MessageBoxImage.Error,
                    new List<MessageBoxOption> { openPurchasesOption, new MessageBoxOption("Ok") });

                if (result == openPurchasesOption)
                    System.Diagnostics.Process.Start("https://groupees.com/purchases");
                return;
            }

            if (!Directory.Exists(installData.InstallLocation))
            {
                installData.InstallLocation = null;
                Plugin.SavePluginSettings(Settings);
                InvokeOnUninstalled(new GameUninstalledEventArgs());
                return;
            }

            var showInstallDirOption = new MessageBoxOption("Show install folder", isDefault: true);
            var markUninstalled = new MessageBoxOption("Mark as uninstalled");
            var dialogResult = PlayniteApi.Dialogs.ShowMessage(
                "Uninstalling Groupees games is manual. Once you've uninstalled the game, you can mark it uninstalled here.",
                $"Uninstall {Game.Name}",
                System.Windows.MessageBoxImage.Question,
                new List<MessageBoxOption> { showInstallDirOption, markUninstalled, new MessageBoxOption("Cancel", isCancel: true) });
            if (dialogResult == showInstallDirOption)
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{installData.InstallLocation}\"");
                Game.IsUninstalling = false;
                PlayniteApi.Database.Games.Update(Game);
            }
            else if (dialogResult == markUninstalled)
            {
                installData.InstallLocation = null;
                Plugin.SavePluginSettings(Settings);
                InvokeOnUninstalled(new GameUninstalledEventArgs());
            }
        }
    }
}