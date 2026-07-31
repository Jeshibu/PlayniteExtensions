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

        var previouslyImported = PlayniteApi.Database.Games.Where(g => g.PluginId == libraryPlugin.Id).ToGroupedDictionary(g => g.GameId);
        var exclusions = PlayniteApi.Database.ImportExclusions.Where(i => i.LibraryId == libraryPlugin.Id).ToGroupedDictionary(i => i.GameId);
        var newlyImported = importGames.ToGroupedDictionary(i => i.GameId);
        var result = new LibraryImportResult
        {
            LibraryPlugin = libraryPlugin,
            MissingGames = previouslyImported.Where(kvp => !newlyImported.ContainsKey(kvp.Key)).SelectMany(kvp => kvp.Value).ToList(),
        };

        foreach (var importGame in importGames)
        {
            if (exclusions.ContainsKey(importGame.GameId))
                result.ExcludedGames.Add(importGame);

            else if (previouslyImported.TryGetValue(importGame.GameId, out var libraryGames))
                result.AlreadyImportedGames.AddRange(libraryGames.Select(lg => new AlreadyImportedGameInfo { ImportedGame = importGame, LibraryGame = lg }));

            else
                result.NewlyImportGames.Add(importGame);
        }

        return result;
    }

    public void TagMissingGames(LibraryImportResult libraryImportResult)
    {
        int newlyTaggedCount = 0, removedTagCount = 0;
        var missingGames = libraryImportResult.MissingGames?.ToGroupedDictionary(g => g.GameId) ?? [];
        var tag = GetTag("Missing from import");
        using (PlayniteApi.Database.BufferedUpdate())
        {
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.PluginId != libraryImportResult.LibraryPlugin.Id)
                    continue;

                bool hasTag = game.TagIds?.Contains(tag.Id) ?? false;
                bool shouldHaveTag = missingGames.ContainsKey(game.GameId);
                if (hasTag == shouldHaveTag)
                    continue;

                if (shouldHaveTag)
                {
                    game.TagIds ??= [];
                    game.TagIds.Add(tag.Id);
                    newlyTaggedCount++;
                }
                else
                {
                    game.TagIds.Remove(tag.Id);
                    removedTagCount++;
                }

                game.Modified = DateTime.Now;
                PlayniteApi.Database.Games.Update(game);
            }
        }

        var filter = PlayniteApi.Dialogs.ShowMessage($"Added tag to {newlyTaggedCount} games, removed it from {removedTagCount} games. Filter to show the games tagged as missing from the library import?",
                                                     "Missing games tagged", MessageBoxButton.YesNo);

        if (filter == MessageBoxResult.Yes)
            PlayniteApi.MainView.ApplyFilterPreset(new FilterPreset { Settings = new()
            {
                Tag = new(tag.Id),
                Library = new(libraryImportResult.LibraryPlugin.Id),
            } });
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

        public GameExportRow(string status, AlreadyImportedGameInfo i) : this(status, i.ImportedGame)
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
