using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

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

        public LegacyGamesInstallController(Game game, LegacyGamesRegistryReader registryReader) : base(game)
        {
            Name = "Install via Legacy Games Launcher";
            this.registryReader = registryReader;
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

            ProcessFocusHelper.SetForeGroundWindow("Legacy Games Launcher", launcherPath);

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

    public static class ProcessFocusHelper
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, EnumForWindow enumVal);

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr parentWindow, IntPtr previousChildWindow, string windowClass, string windowTitle);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr window, out int process);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SWP uFlags);

        [Flags]
        private enum SWP
        {
            ASYNCWINDOWPOS = 0x4000,
            DEFERERASE = 0x2000,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            HIDEWINDOW = 0x0080,
            NOACTIVATE = 0x0010,
            NOCOPYBITS = 0x0100,
            NOMOVE = 0x0002,
            NOOWNERZORDER = 0x0200,
            NOREDRAW = 0x0008,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            NOSIZE = 0x0001,
            NOZORDER = 0x0004,
            SHOWWINDOW = 0x0040,
            TOPMOST = NOACTIVATE | NOOWNERZORDER | NOSIZE | NOMOVE | NOREDRAW | NOSENDCHANGING
        }

        private enum EnumForWindow
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        public static void SetForeGroundWindow(string processName, string processStartPath)
        {
            var p = Process.GetProcessesByName(processName).FirstOrDefault();

            if (p != null)
            {
                var windowHandles = GetProcessWindows(p.Id);

                foreach (var wh in windowHandles)
                {
                    ShowWindow(wh, EnumForWindow.ShowNormal);
                    Thread.Sleep(1000);
                    SetForegroundWindow(wh);
                    Thread.Sleep(1000);
                    SetWindowPos(wh, IntPtr.Zero, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.SHOWWINDOW);
                }
            }
            else
            {
                Process.Start(processStartPath);
            }
        }


        private static IntPtr[] GetProcessWindows(int process)
        {
            IntPtr[] apRet = new IntPtr[256];
            int iCount = 0;
            IntPtr pLast = IntPtr.Zero;
            do
            {
                pLast = FindWindowEx(IntPtr.Zero, pLast, null, null);
                int iProcess_;
                GetWindowThreadProcessId(pLast, out iProcess_);
                if (iProcess_ == process) apRet[iCount++] = pLast;
            } while (pLast != IntPtr.Zero);
            Array.Resize(ref apRet, iCount);
            return apRet;
        }
    }

    public static class ProcessFocusHelper2
    {
        private const string dllName = "User32.dll";
        [DllImport(dllName)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport(dllName)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport(dllName)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport(dllName)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport(dllName, EntryPoint = "SetWindowPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SWP uFlags);

        private static void SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SWP uFlags)
        {
            if (!_SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, uFlags))
            {
                throw new Win32Exception();
            }
        }

        [Flags]
        private enum SWP
        {
            ASYNCWINDOWPOS = 0x4000,
            DEFERERASE = 0x2000,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            HIDEWINDOW = 0x0080,
            NOACTIVATE = 0x0010,
            NOCOPYBITS = 0x0100,
            NOMOVE = 0x0002,
            NOOWNERZORDER = 0x0200,
            NOREDRAW = 0x0008,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            NOSIZE = 0x0001,
            NOZORDER = 0x0004,
            SHOWWINDOW = 0x0040,
            TOPMOST = NOACTIVATE | NOOWNERZORDER | NOSIZE | NOMOVE | NOREDRAW | NOSENDCHANGING
        }

        public static void FocusWindow(Window window)
        {
            var interopHelper = new WindowInteropHelper(window);
            var thisWindowThreadId = GetWindowThreadProcessId(interopHelper.Handle, IntPtr.Zero);

            //Get the process ID for the foreground window's thread
            var currentForegroundWindow = GetForegroundWindow();
            var currentForegroundWindowThreadId = GetWindowThreadProcessId(currentForegroundWindow, IntPtr.Zero);

            //Attach this window's thread to the current window's thread
            AttachThreadInput(currentForegroundWindowThreadId, thisWindowThreadId, true);

            //Set the window position
            SetWindowPos(interopHelper.Handle, IntPtr.Zero, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.SHOWWINDOW);

            //Detach this window's thread from the current window's thread
            AttachThreadInput(currentForegroundWindowThreadId, thisWindowThreadId, false);

        }
    }
}