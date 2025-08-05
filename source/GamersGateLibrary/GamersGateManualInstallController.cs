using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GamersGateLibrary;

public class GamersGateManualInstallController(Game game, GamersGateLibrarySettings settings, IPlayniteAPI playniteAPI, Plugin plugin) : InstallController(game)
{
    public GamersGateLibrarySettings Settings { get; } = settings;
    public IPlayniteAPI PlayniteAPI { get; } = playniteAPI;
    public Plugin Plugin { get; } = plugin;

    private readonly ILogger logger = LogManager.GetLogger();

    public override void Install(InstallActionArgs args)
    {
        if (!Settings.InstallData.TryGetValue(Game.GameId, out var installData))
        {
            logger.Debug($"No install data found for {Game.Name}, ID: {Game.GameId}");
            var openPurchasesOption = new MessageBoxOption("Open orders page");
            var result = PlayniteAPI.Dialogs.ShowMessage(
                "No install/download data found for this game. Please re-run the game import after checking your orders page.",
                "Install data missing",
                System.Windows.MessageBoxImage.Error,
                [openPurchasesOption, new MessageBoxOption("Ok")]);

            if (result == openPurchasesOption)
                System.Diagnostics.Process.Start("https://www.gamersgate.com/account/orders/");
            return;
        }

        var downloadOption = new MessageBoxOption("Download", isDefault: true);
        var selectInstallFolderOption = new MessageBoxOption("Select install folder");
        var dialogResult = PlayniteAPI.Dialogs.ShowMessage(
            "Installation for GamersGate games is manual. Downloading is done through your browser and requires being logged in to gamersgate.com. Once you have installed the game, select the install folder here.",
            $"Install {Game.Name}",
            System.Windows.MessageBoxImage.Question,
            [downloadOption, selectInstallFolderOption, new MessageBoxOption("Cancel", isCancel: true)]);

        var downloadUrl = $"https://www.gamersgate.com/account/orders/{installData.OrderId}/#{installData.Id}";

        if (dialogResult == downloadOption)
        {
            Game.IsInstalling = false;
            System.Diagnostics.Process.Start(downloadUrl);
        }
        else if (dialogResult == selectInstallFolderOption)
        {
            var installationDirectory = PlayniteAPI.Dialogs.SelectFolder();
            if (string.IsNullOrWhiteSpace(installationDirectory))
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
            Game.IsInstalled = false;
            Game.IsInstalling = false;
            Game.InstallDirectory = null;
        }
    }
}