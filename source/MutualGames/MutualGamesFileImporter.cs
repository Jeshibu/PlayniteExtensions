using MutualGames.Models.Export;
using MutualGames.Models.Settings;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MutualGames;

public sealed class MutualGamesFileImporter : MutualGamesBaseImporter
{
    private readonly IPlatformUtility platformUtility;
    private readonly TitleComparer titleComparer = new TitleComparer();

    public MutualGamesFileImporter(IPlayniteAPI playniteAPI, MutualGamesSettings settings, IPlatformUtility platformUtility) : base(playniteAPI, settings)
    {
        this.platformUtility = platformUtility;
    }

    public void Import()
    {
        if (!TryGetFile(out var file))
            return;

        var friendName = GetFriendName(file);
        if (friendName == null)
            return;

        var result = playniteAPI.Dialogs.ActivateGlobalProgress(a =>
        {
            try
            {
                a.Text = $"Reading {file.FullName}";
                var fileContentString = File.ReadAllText(file.FullName);
                var fileContent = JsonConvert.DeserializeObject<ExportRoot>(fileContentString);

                var pluginsById = fileContent.LibraryPlugins.ToDictionary(p => p.Id, p => p.Name);
                var platformsById = fileContent.Platforms.ToDictionary(p => p.Id);
                var gamesByPluginId = fileContent.Games.GroupBy(g => g.PluginId).ToList();

                a.ProgressMaxValue = gamesByPluginId.Count() + 1;
                a.CurrentProgressValue = 1;

                matchingHelper.GetDeflatedNames(playniteAPI.Database.Games.Select(g => g.Name));
                matchingHelper.GetDeflatedNames(fileContent.Games.Select(g => g.Name));

                string GetPluginName(Guid pluginId)
                {
                    if (pluginId == default)
                        return "Playnite";

                    if (pluginsById.TryGetValue(pluginId, out var pluginName))
                        return pluginName;

                    return "UNKNOWN PLUGIN";
                }

                using (playniteAPI.Database.BufferedUpdate())
                {
                    foreach (var grouping in gamesByPluginId)
                    {
                        if (a.CancelToken.IsCancellationRequested) break;

                        string pluginName = GetPluginName(grouping.Key);

                        var dbItem = GetDatabaseItem(friendName, pluginName);

                        var friendGames = grouping.ToList();
                        a.Text = $"Importing {friendGames.Count} from {pluginName} for {friendName}";

                        try
                        {
                            var matchingGames = grouping.Key == default
                                    ? GetPlayniteMatchingGames(friendGames, platformsById)
                                    : GetMatchingGames(grouping.Key, friendGames, friendName);

                            foreach (var matchingGame in matchingGames)
                                if (AddPropertyToGame(matchingGame, dbItem))
                                    playniteAPI.Database.Games.Update(matchingGame);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Error while getting games for {pluginName} for {friendName}");
                        }
                        a.CurrentProgressValue++;
                    }
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex, "Error importing file");
                playniteAPI.Notifications.Add(new NotificationMessage("mutualgames-file-import-error", $"Erorr while importing Mutual Games file: {ex.Message}", NotificationType.Error));
            }
        }, new GlobalProgressOptions("Importing friend games", cancelable: true) { IsIndeterminate = false });

        playniteAPI.Dialogs.ShowMessage($"Imported {updatedCount} new friends' games.", "Mutual Games import done");
    }

    private bool TryGetFile(out FileInfo file)
    {
        file = null;

        var filePath = playniteAPI.Dialogs.SelectFile(MutualGamesHelper.ExportFileFilter);
        if (filePath == null)
            return false;

        file = new FileInfo(filePath);
        return file.Exists;
    }

    private string GetFriendName(FileInfo file)
    {
        string examplePropertyName = MutualGamesHelper.GetPropertyName(settings.PropertyNameFormat, "FRIENDNAME", "LIBRARY");
        string defaultFriendName = file.Name.TrimEnd(file.Extension);
        var friendNameResult = playniteAPI.Dialogs.SelectString($"Enter your friend's name. Mutual games will be added to {settings.ImportTo} named \"{examplePropertyName}\"", "Enter friend name", defaultFriendName);
        if (!friendNameResult.Result || string.IsNullOrWhiteSpace(friendNameResult.SelectedString))
            return null;

        return friendNameResult.SelectedString;
    }

    private IEnumerable<Game> GetPlayniteMatchingGames(IEnumerable<ExternalGameData> friendGames, IDictionary<Guid, PlatformData> platforms)
    {
        var output = new List<Game>();
        var localGames = GetPlayniteLibraryPotentialMatches();

        foreach (var friendGame in friendGames)
        {
            var deflatedFriendGameName = matchingHelper.GetDeflatedName(friendGame.Name);
            foreach (var localGame in localGames)
            {
                var deflatedLocalGameName = matchingHelper.GetDeflatedName(localGame.Name);
                bool namesMatch = deflatedFriendGameName.Equals(deflatedLocalGameName, StringComparison.InvariantCultureIgnoreCase);
                if (namesMatch && (HasRelevantFeature(localGame) || PlatformsOverlap(friendGame, localGame, platforms)))
                    output.Add(localGame);
            }
        }

        return output;
    }

    private IEnumerable<Game> GetPlayniteLibraryPotentialMatches()
    {
        var games = playniteAPI.Database.Games;
        switch (settings.CrossLibraryImportMode)
        {
            case CrossLibraryImportMode.SameLibraryOnly:
                return games.Where(g => g.PluginId == default);
            case CrossLibraryImportMode.ImportAllWithFeature:
                return games.Where(g => g.PluginId == default || HasRelevantFeature(g));
            case CrossLibraryImportMode.ImportAll:
            default:
                return games;
        }
    }

    private bool HasRelevantFeature(Game game)
    {
        return settings.CrossLibraryImportMode == CrossLibraryImportMode.ImportAllWithFeature
            && game.FeatureIds?.Contains(settings.ImportCrossLibraryFeatureId) == true;
    }

    private bool PlatformsOverlap(ExternalGameData friendGame, Game game, IDictionary<Guid, PlatformData> platforms)
    {
        if (!settings.LimitPlayniteLibraryGamesToSamePlatform)
            return true;

        var gamePlatforms = game.Platforms;
        if (friendGame.PlatformIds == null || gamePlatforms == null || gamePlatforms.Count == 0)
            return true;

        var friendGamePlatforms = new List<PlatformData>();
        foreach (var platformId in friendGame.PlatformIds)
            if (platforms.TryGetValue(platformId, out var platformData))
                friendGamePlatforms.Add(platformData);

        if (friendGamePlatforms.Count == 0)
            return true;

        bool GameHasPlatformSpecId(string specId) => !string.IsNullOrWhiteSpace(specId) && gamePlatforms.Any(gp => gp.SpecificationId == specId);
        bool GameHasPlatformName(string platformName) => !string.IsNullOrWhiteSpace(platformName) && gamePlatforms.Any(gp => titleComparer.Equals(platformName, gp.Name));

        foreach (var fgp in friendGamePlatforms)
        {
            if (!string.IsNullOrEmpty(fgp.SpecificationId))
            {
                if (GameHasPlatformSpecId(fgp.SpecificationId))
                    return true;
            }
            else
            {
                var parsedFriendGamePlatforms = platformUtility.GetPlatforms(fgp.Name);
                foreach (var pfgp in parsedFriendGamePlatforms)
                {
                    if (pfgp is MetadataSpecProperty sp)
                    {
                        if (GameHasPlatformSpecId(sp.Id))
                            return true;
                    }
                    else if (pfgp is MetadataNameProperty np)
                    {
                        if (GameHasPlatformName(np.Name))
                            return true;
                    }
                }
            }
        }
        return false;
    }
}
