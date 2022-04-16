using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GroupeesLibrary
{
    public class GroupeesManualInstallController : InstallController
    {
        public GroupeesManualInstallController(Game game, GroupeesLibrarySettings settings, IPlayniteAPI playniteAPI, Plugin plugin) : base(game)
        {
            Settings = settings;
            PlayniteAPI = playniteAPI;
            Plugin = plugin;
        }

        public GroupeesLibrarySettings Settings { get; }
        public IPlayniteAPI PlayniteAPI { get; }
        public Plugin Plugin { get; }

        private ILogger logger = LogManager.GetLogger();

        public override void Install(InstallActionArgs args)
        {
            if (!Settings.InstallData.TryGetValue(Game.GameId, out var installData))
            {
                logger.Debug($"No install data found for {Game.Name}, ID: {Game.GameId}");
                var openPurchasesOption = new MessageBoxOption("Open purchases page");
                var result = PlayniteAPI.Dialogs.ShowMessage(
                    "No install/download data found for this game. Please re-run the game import after checking your purchases page.",
                    "Install data missing",
                    System.Windows.MessageBoxImage.Error,
                    new List<MessageBoxOption> { openPurchasesOption, new MessageBoxOption("Ok") });

                if (result == openPurchasesOption)
                    System.Diagnostics.Process.Start("https://groupees.com/purchases");
                return;
            }

            var downloadOption = new MessageBoxOption("Download", isDefault: true);
            var selectInstallFolderOption = new MessageBoxOption("Select install folder");
            var dialogResult = PlayniteAPI.Dialogs.ShowMessage(
                "Installation for Groupees games is manual. Downloading is done through your browser and requires being logged in to groupees.com. Once you have installed the game, select the install folder here.",
                $"Install {Game.Name}",
                System.Windows.MessageBoxImage.Question,
                new List<MessageBoxOption> { downloadOption, selectInstallFolderOption, new MessageBoxOption("Cancel", isCancel: true) });

            if (dialogResult == downloadOption)
            {
                Game.IsInstalling = false;
                System.Diagnostics.Process.Start(installData.DownloadUrl);
            }
            else if (dialogResult == selectInstallFolderOption)
            {
                var installationDirectory = PlayniteAPI.Dialogs.SelectFolder();
                if (string.IsNullOrEmpty(installationDirectory))
                {
                    Game.IsInstalling = false;
                    logger.Debug("User cancelled out of install directory selection");
                    InvokeOnInstalled(new GameInstalledEventArgs(new GameInstallationData { InstallDirectory = null }));
                    return;
                }

                string[] exePaths = Directory.GetFiles(installationDirectory, "*.exe", SearchOption.AllDirectories);
                var exeOptions = exePaths.Select(s => new GenericItemOption { Name = s.Replace(installationDirectory, string.Empty).TrimStart('\\') }).ToList();

                var selectedExe = PlayniteAPI.Dialogs.ChooseItemWithSearch(exeOptions,
                    (s) => string.IsNullOrWhiteSpace(s) ? exeOptions : exeOptions.Where(o => o.Name.Contains(s)).ToList(),
                    caption: $"Please select the .exe file that starts {Game.Name} in {installationDirectory}.");

                if (selectedExe == null)
                {
                    Game.IsInstalling = false;
                    logger.Debug("User cancelled out of game exe selection");
                    InvokeOnInstalled(new GameInstalledEventArgs(new GameInstallationData { InstallDirectory = null }));
                    return;
                }
                installData.InstallLocation = installationDirectory;
                installData.RelativeExecutablePath = selectedExe.Name;
                Plugin.SavePluginSettings(Settings);
                InvokeOnInstalled(new GameInstalledEventArgs(new GameInstallationData { InstallDirectory = installationDirectory }));
            }
            else if (dialogResult.IsCancel)
            {
                InvokeOnInstalled(new GameInstalledEventArgs(new GameInstallationData { InstallDirectory = null }));
            }
        }
    }
}