using CsvHelper;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace ImportAnalyzer;

public class ImportAnalyzerPlugin(IPlayniteAPI playniteApi) : GenericPlugin(playniteApi)
{
    private readonly ILogger logger = LogManager.GetLogger();
    public override Guid Id { get; } = new("1305C6D9-9528-4051-81C2-6813DDF191BA");

    public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
    {
        if (PlayniteApi.ApplicationInfo.Mode != ApplicationMode.Desktop)
            yield break;

        var libraryPlugins = PlayniteApi.Addons.Plugins.OfType<LibraryPlugin>().ToList();
        foreach (var libraryPlugin in libraryPlugins)
            yield return new() { MenuSection = "@Import Analyzer", Description = libraryPlugin.Name, Icon = libraryPlugin.LibraryIcon, Action = _ => StartAnalysis(libraryPlugin) };
    }

    private void StartAnalysis(LibraryPlugin libraryPlugin)
    {
        if (libraryPlugin == null)
            throw new ArgumentNullException(nameof(libraryPlugin));

        LibraryImportResult importResult = null;
        PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
        {
            try
            {
                importResult = ImportFromLibrary(libraryPlugin);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error running GetGames for {libraryPlugin.Name}");
                PlayniteApi.Dialogs.ShowErrorMessage($"Error getting games for {libraryPlugin.Name}: {ex}", "Error");
            }
        }, new($"Importing games from {libraryPlugin.Name} to analyze...") { IsIndeterminate = true });

        if (importResult == null)
            return;

        var window = PlayniteApi.Dialogs.CreateWindow(new() { ShowCloseButton = true, ShowMaximizeButton = true, ShowMinimizeButton = true });
        window.Content = new ImportResultView(this, importResult, window);
        window.SizeToContent = SizeToContent.WidthAndHeight;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.Title = $"{libraryPlugin.Name} import result";
        window.Show();
    }

    private LibraryImportResult ImportFromLibrary(LibraryPlugin libraryPlugin)
    {
        var importGames = libraryPlugin.GetGames(new())?.OrderBy(g => g.Name).ToList();
        if (importGames == null)
            return null;

        var previouslyImported = PlayniteApi.Database.Games.Where(g => g.PluginId == libraryPlugin.Id).ToDictionary(g => g.GameId);
        var exclusions = PlayniteApi.Database.ImportExclusions.Where(i => i.LibraryId == libraryPlugin.Id).ToDictionary(i => i.GameId);
        var newlyImported = importGames.ToDictionary(g => g.GameId);
        var result = new LibraryImportResult
        {
            MissingGames = previouslyImported.Values.Where(g => !newlyImported.ContainsKey(g.GameId)).ToList(),
        };

        foreach (var importGame in importGames)
        {
            if (exclusions.ContainsKey(importGame.GameId))
                result.ExcludedGames.Add(importGame);

            else if (previouslyImported.TryGetValue(importGame.GameId, out var libraryGame))
                result.AlreadyImportedGames.Add(new() { ImportedGame = importGame, LibraryGame = libraryGame });

            else
                result.NewlyImportGames.Add(importGame);
        }

        return result;
    }

    public void TagMissingGames(LibraryImportResult libraryImportResult)
    {
        if (libraryImportResult?.MissingGames?.Any() != true)
            return;

        int newlyTaggedCount = 0;
        var tag = GetTag("Missing from import");
        using (PlayniteApi.Database.BufferedUpdate())
        {
            foreach (var missingGame in libraryImportResult.MissingGames)
            {
                if (missingGame.TagIds?.Contains(tag.Id) == true)
                    continue;

                missingGame.TagIds ??= [];
                missingGame.TagIds.Add(tag.Id);
                missingGame.Modified = DateTime.Now;
                PlayniteApi.Database.Games.Update(missingGame);
                newlyTaggedCount++;
            }
        }

        var filter = PlayniteApi.Dialogs.ShowMessage($"Tagged {newlyTaggedCount} games. Filter to show the games tagged as missing from the library import?",
                                                     "Missing games tagged", MessageBoxButton.YesNo);

        if (filter == MessageBoxResult.Yes)
            PlayniteApi.MainView.ApplyFilterPreset(new FilterPreset { Settings = new() { Tag = new(tag.Id) } });
    }

    private Tag GetTag(string name)
    {
        var tag = PlayniteApi.Database.Tags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        if (tag != null)
            return tag;

        return PlayniteApi.Database.Tags.Add(name);
    }

    public bool ExportImportResult(LibraryImportResult libraryImportResult)
    {
        string targetPath = PlayniteApi.Dialogs.SaveFile("Comma-separated values|*.csv", true, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        if (string.IsNullOrWhiteSpace(targetPath))
            return false;

        try
        {
            List<GameExportRow> rows =
            [
                .. libraryImportResult.NewlyImportGames.Select(g => new GameExportRow("New", g)),
                .. libraryImportResult.AlreadyImportedGames.Select(g => new GameExportRow("Already imported", g)),
                .. libraryImportResult.ExcludedGames.Select(g => new GameExportRow("Excluded", g)),
                .. libraryImportResult.MissingGames.Select(g => new GameExportRow("Missing", g))
            ];

            using var writer = new StreamWriter(targetPath, false);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteHeader<GameExportRow>();
            csv.NextRecord();
            csv.WriteRecords(rows);

            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error exporting library import analysis");
            return false;
        }
    }

    private class GameExportRow
    {
        public string Status { get; }
        public string ID { get; }
        public string Name { get; }
        public bool Installed { get; }
        public string Source { get; }
        public ulong Playtime { get; }
        public DateTime? LastPlayed { get; }
        public string DifferentNameInLibrary { get; }

        public GameExportRow(string status, GameMetadata m)
        {
            Status = status;
            ID = m.GameId;
            Name = m.Name;
            Installed = m.IsInstalled;
            Source = m.Source?.ToString();
            Playtime = m.Playtime;
            LastPlayed = m.LastActivity;
        }

        public GameExportRow(string status, AlreadyImportedGameInfo i): this(status, i.ImportedGame)
        {
            if (i.LibraryGame.Name != i.ImportedGame.Name)
                DifferentNameInLibrary = i.LibraryGame.Name;
        }

        public GameExportRow(string status, Game g)
        {
            Status = status;
            ID = g.GameId;
            Name = g.Name;
            Installed = g.IsInstalled;
            Source = g.Source?.Name;
            Playtime = g.Playtime;
            LastPlayed = g.LastActivity;
        }
    }
}
