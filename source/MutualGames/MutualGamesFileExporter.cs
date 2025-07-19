using Microsoft.Win32;
using MutualGames.Models.Export;
using MutualGames.Models.Settings;
using MutualGames.Views.Export;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace MutualGames;

public sealed class MutualGamesFileExporter
{
    private IPlayniteAPI PlayniteAPI { get; }
    private MutualGamesSettings Settings { get; }

    public MutualGamesFileExporter(IPlayniteAPI playniteAPI, MutualGamesSettings settings)
    {
        PlayniteAPI = playniteAPI;
        Settings = settings;
    }

    public void Export()
    {
        var promptResult = Prompt();
        if (promptResult == null)
            return;

        if (!TryGetExportFilePath(out var filePath))
            return;

        var games = GetGames(promptResult.Mode).Select(ExternalGameData.FromGame);
        var plugins = PlayniteAPI.Addons.Plugins.OfType<LibraryPlugin>().Select(PluginData.FromPlugin);
        var platforms = PlayniteAPI.Database.Platforms.Select(PlatformData.FromPlatform);

        var root = new ExportRoot();
        root.Games.AddRange(games);
        root.LibraryPlugins.AddRange(plugins);
        root.Platforms.AddRange(platforms);

        File.WriteAllText(filePath, JsonConvert.SerializeObject(root, Formatting.None));
        PlayniteAPI.Dialogs.ShowMessage($"Exported {root.Games.Count} games! Send the file to friends to let them mark your mutual games.");
    }

    private ExportFilePromptViewModel Prompt()
    {
        var window = PlayniteAPI.Dialogs.CreateWindow(new WindowCreationOptions { ShowCloseButton = true, ShowMinimizeButton = false, ShowMaximizeButton = false });
        var promptViewModel = new ExportFilePromptViewModel();
        var view = new ExportFilePromptView(window) { DataContext = promptViewModel };
        window.Content = view;
        window.SizeToContent = SizeToContent.WidthAndHeight;
        window.Owner = PlayniteAPI.Dialogs.GetCurrentAppWindow();
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.Title = "Export games for Mutual Games";
        var result = window.ShowDialog();
        if (result == false)
            return null;

        return promptViewModel;
    }

    private bool TryGetExportFilePath(out string filePath)
    {
        var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var fileName = Environment.UserName + ".mutualgames";

        SaveFileDialog saveFileDialog1 = new SaveFileDialog
        {
            Filter = MutualGamesHelper.ExportFileFilter,
            Title = "Save Mutual Games export file",
            InitialDirectory = myDocuments,
            FileName = fileName,
        };
        var result = saveFileDialog1.ShowDialog();

        filePath = saveFileDialog1.FileName;
        return (result ?? false) && !string.IsNullOrWhiteSpace(filePath);
    }

    private IEnumerable<Game> GetGames(ExportGamesMode mode)
    {
        return mode switch
        {
            ExportGamesMode.AllIncludeHidden => PlayniteAPI.Database.Games,
            ExportGamesMode.AllExcludeHidden => PlayniteAPI.Database.Games.Where(g => !g.Hidden),
            ExportGamesMode.Filtered => PlayniteAPI.MainView.FilteredGames,
            _ => throw new ArgumentException(nameof(mode)),
        };
    }
}
