using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyGamesLibrary
{
    public class LegacyGamesUninstallController : UninstallController
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private CancellationTokenSource watcherToken;
        private LegacyGamesRegistryReader registryReader;

        public LegacyGamesUninstallController(Game game, LegacyGamesRegistryReader registryReader) : base(game)
        {
            Name = "Uninstall using Legacy Games uninstaller";
            this.registryReader = registryReader;
        }

        public override void Dispose()
        {
            watcherToken?.Cancel();
        }

        public override void Uninstall(UninstallActionArgs args)
        {
            var uninstallerPath = Path.Combine(Game.InstallDirectory, "Uninstall.exe");
            if (!File.Exists(uninstallerPath))
                throw new Exception("Can't uninstall game. Uninstall.exe not found in game directory.");

            System.Diagnostics.Process.Start(uninstallerPath);
            StartUninstallWatcher();
        }

        public async void StartUninstallWatcher()
        {
            watcherToken = new CancellationTokenSource();

            while (true)
            {
                if (watcherToken.IsCancellationRequested)
                {
                    return;
                }

                IEnumerable<RegistryGameData> installedGames = null;
                Guid gameId = Guid.Empty;
                try
                {
                    installedGames = registryReader.GetGameData(Microsoft.Win32.RegistryView.Default);
                    gameId = Guid.Parse(Game.GameId);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to get info about installed Legacy Games Launcher games.");
                }

                if (installedGames != null)
                {
                    if (!installedGames.Any(g => g.InstallerUUID == gameId))
                    {
                        InvokeOnUninstalled(new GameUninstalledEventArgs());
                        return;
                    }
                }

                await Task.Delay(2000);
            }
        }
    }


    public class LegacyGamesInstallController : InstallController
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private CancellationTokenSource watcherToken;
        private LegacyGamesRegistryReader registryReader;
        private readonly IPlayniteAPI playniteAPI;

        public LegacyGamesInstallController(Game game, LegacyGamesRegistryReader registryReader, IPlayniteAPI playniteAPI) : base(game)
        {
            Name = "Install via Legacy Games Launcher";
            this.registryReader = registryReader;
            this.playniteAPI = playniteAPI;
        }

        public override void Dispose()
        {
            watcherToken?.Cancel();
        }

        public override void Install(InstallActionArgs args)
        {
            var launcherPath = registryReader.GetLauncherPath();
            if (!File.Exists(launcherPath))
                throw new Exception("Can't launch Legacy Games Launcher. Installation not found.");

            playniteAPI.Dialogs.ShowMessage("Use the Legacy Games Launcher to install the game. \nIt will be properly marked as installed in Playnite automatically.", "User action required");

            Process.Start(launcherPath);

            StartInstallWatcher();
        }

        public async void StartInstallWatcher()
        {
            watcherToken = new CancellationTokenSource();

            while (true)
            {
                if (watcherToken.IsCancellationRequested)
                {
                    return;
                }

                IEnumerable<RegistryGameData> installedGames = null;
                Guid gameId = Guid.Empty;
                try
                {
                    installedGames = registryReader.GetGameData(Microsoft.Win32.RegistryView.Default);
                    gameId = Guid.Parse(Game.GameId);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to get info about installed Legacy Games Launcher games.");
                }

                if (installedGames != null)
                {
                    var installedGame = installedGames.FirstOrDefault(g => g.InstallerUUID == gameId);
                    if (installedGame?.InstDir != null)
                    {
                        InvokeOnInstalled(new GameInstalledEventArgs(new GameInstallationData { InstallDirectory = installedGame.InstDir }));
                        return;
                    }
                }

                await Task.Delay(2000);
            }
        }
    }
}