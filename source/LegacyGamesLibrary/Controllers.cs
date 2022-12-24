using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

            FocusWindow();
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

        private void FocusWindow()
        {
            // Get Steam's process ID for comparison with child process parent IDs
            var process = Process.GetProcessesByName("Legacy Games Launcher")?.FirstOrDefault();
            if (process == null)
            {
                logger.Info("Couldn't focus window - Legacy Games Launcher isn't running");
                return;
            }

            var windows = GetProcessWindows(process);

            if (windows.Count == 0)
            {
                logger.Info("No windows found to focus.");
                SetForegroundWindow(process.MainWindowHandle);
                return;
            }

            foreach (var window in windows)
            {
                // Wait for the previous focus call to resolve
                // Otherwise this one might be ignored
                Thread.Sleep(10);
                logger.Debug($"Setting foreground window: {window.Handle} {window.Title}");
                SetForegroundWindow(window.Handle);
            }
        }

        #region FindWindows

        private class WindowInfo
        {
            public WindowInfo(IntPtr handle, string title)
            {
                Handle = handle;
                Title = title;
            }

            public IntPtr Handle { get; set; }
            public string Title { get; set; }
        }

        private static List<WindowInfo> GetProcessWindows(Process process)
        {
            var windows = new List<WindowInfo>();

            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                GetWindowThreadProcessId(hWnd, out uint processId);
                if (process.Id != processId)
                {
                    // Return true here so that we iterate all windows
                    return true;
                }

                var text = GetWindowText(hWnd);

                windows.Add(new WindowInfo(hWnd, text));
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary> Get the text for the window pointed to by hWnd </summary>
        private static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return string.Empty;
        }

        #endregion FindWindows

        #region PrivateImports

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion
    }
}