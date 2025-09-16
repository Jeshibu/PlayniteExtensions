using Playnite.SDK;
using System;
using System.Windows;

namespace FilterSearch.Helpers;

public static class WindowHelper
{
    private static readonly ILogger logger = LogManager.GetLogger();
    private static Window GetMainWindow() => Application.Current.MainWindow;
    private static WindowState LastState { get; set; } = WindowState.Normal;

    public static void Init()
    {
        var mainWindow = GetMainWindow();
        mainWindow.StateChanged += MainWindowOnStateChanged;
        UpdateLastState(mainWindow);
    }

    public static void BringMainWindowToForeground()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            logger.Error("No main window found.");
            return;
        }

        logger.Info($"Initial window state: {mainWindow.WindowState}");


        if (mainWindow.WindowState == WindowState.Minimized)
        {
            //Hack to restore window to foreground when minimized https://stackoverflow.com/a/11941579
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.Show();
            mainWindow.WindowState = LastState;
        }
        else
        {
            mainWindow.Activate();
        }

        mainWindow.Focus();
    }

    private static void MainWindowOnStateChanged(object sender, EventArgs e)
    {
        if (sender is not Window window)
        {
            logger.Error($"Sender is not a window: {sender?.GetType()}");
            return;
        }

        UpdateLastState(window);
    }

    private static void UpdateLastState(Window window)
    {
        if (window.WindowState != WindowState.Minimized)
            LastState = window.WindowState;
    }
}