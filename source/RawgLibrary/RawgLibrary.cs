using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using Rawg.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;

namespace RawgLibrary;

public class RawgLibrary : LibraryPlugin
{
    private readonly ILogger logger = LogManager.GetLogger();
    private static readonly string iconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.jpg");
    public override string LibraryIcon { get; } = iconPath;

    private RawgLibrarySettingsViewModel settings { get; set; }

    private RawgApiClient rawgApiClient = null;

    public override Guid Id { get; } = RawgMetadataHelper.RawgLibraryId;

    public override string Name { get; } = "RAWG";

    private TitleComparer TitleComparer = new TitleComparer();

    public RawgLibrary(IPlayniteAPI api) : base(api)
    {
        settings = new RawgLibrarySettingsViewModel(this);
        Properties = new LibraryPluginProperties
        {
            HasSettings = true
        };
        api.Database.Games.ItemUpdated += Games_ItemUpdated;
        api.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
    }

    private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
    {
        if (!settings.Settings.AutoSyncDeletedGames)
            return;

        var client = GetApiClient();
        var token = settings.Settings.UserToken;
        if (token == null)
        {
            string error = "Could not automatically sync game updates: no user token";
            logger.Warn(error);
            PlayniteApi.Notifications.Add("rawg-sync-error", error, NotificationType.Error);
            return;
        }

        PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
        {
            var removedItems = e.RemovedItems ?? new List<Game>();
            a.ProgressMaxValue = removedItems.Count;
            try
            {

                if (settings.Settings.AutoSyncDeletedGames)
                {
                    foreach (var game in removedItems)
                    {
                        if (a.CancelToken.IsCancellationRequested)
                            break;

                        a.CurrentProgressValue++;

                        DeleteGameFromRawgLibrary(game, client, token);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error syncing new/deleted games to RAWG library");
                PlayniteApi.Notifications.Add("rawg-sync-error", $"Error syncing new/deleted to RAWG library: {ex?.Message}", NotificationType.Error);
            }
        }
        , new GlobalProgressOptions("Syncing new/deleted games to RAWG library", cancelable: true) { IsIndeterminate = false });
    }

    private void DeleteGameFromRawgLibrary(Game game, RawgApiClient client, string token)
    {
        if (DuplicateExists(game))
            return; //skip delete if there's any duplicates left in the Playnite library

        if (!TryGetRawgIdFromGame(game, client, setLink: false, out var gameId))
            return;

        if (client.DeleteGameFromLibrary(token, gameId))
            logger.Info($"Deleted {game.Name} from RAWG library (RAWG id {gameId})");
        else
            logger.Info($"Could not delete {game.Name} from RAWG library (RAWG id {gameId})");
    }

    private bool DuplicateExists(Game game)
    {
        foreach (var g in PlayniteApi.Database.Games)
        {
            if (game.Id == g.Id) continue;

            if (g.Hidden && !settings.Settings.AutoSyncHiddenGames) continue;

            if (TitleComparer.Compare(g.Name, game.Name) == 0) return true;
        }
        return false;
    }

    private void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
    {
        if (!settings.Settings.AutoSyncCompletionStatus && !settings.Settings.AutoSyncUserScore && !settings.Settings.AutoSyncNewGames)
            return;

        var client = GetApiClient();
        var token = settings.Settings.UserToken;
        if (token == null)
        {
            string error = "Could not automatically sync game updates: no user token";
            logger.Warn(error);
            PlayniteApi.Notifications.Add("rawg-sync-error", error, NotificationType.Error);
            return;
        }

        PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
        {
            a.ProgressMaxValue = e.UpdatedItems.Count;
            try
            {
                foreach (var item in e.UpdatedItems)
                {
                    if (a.CancelToken.IsCancellationRequested)
                        break;

                    a.CurrentProgressValue++;

                    int gameId = -1;

                    //skip hidden games if applicable
                    if (!settings.Settings.AutoSyncHiddenGames && item.OldData.Hidden && item.NewData.Hidden)
                        continue;

                    //sync-delete newly hidden games
                    if (!settings.Settings.AutoSyncHiddenGames && !item.OldData.Hidden && item.NewData.Hidden)
                    {
                        DeleteGameFromRawgLibrary(item.NewData, client, token);

                        continue;
                    }

                    var syncCompletionStatusUpdate = settings.Settings.AutoSyncCompletionStatus
                        && (item.OldData.CompletionStatusId != item.NewData.CompletionStatusId || item.OldData.Hidden && !item.NewData.Hidden);

                    var syncNewGame = settings.Settings.AutoSyncNewGames
                        && item.OldData.PluginId == Guid.Empty
                        && item.OldData.Name == "New Game"
                        && item.OldData.Name != item.NewData.Name;

                    if (syncCompletionStatusUpdate || syncNewGame)
                    {
                        if (!TryGetRawgIdFromGame(item.NewData, client, true, out gameId))
                            continue;

                        if (SyncCompletionStatus(item.NewData, gameId, client, token))
                            logger.Info($"Synced {item.NewData.Name} (RAWG id {gameId}) to status {item.NewData.CompletionStatus?.Name}");
                        else
                            logger.Info($"Could not sync {item.NewData.Name} (RAWG id {gameId}) to status {item.NewData.CompletionStatus?.Name}");
                    }

                    if (a.CancelToken.IsCancellationRequested)
                        break;

                    if (settings.Settings.AutoSyncUserScore && item.OldData.UserScore != item.NewData.UserScore)
                    {
                        if (gameId == -1 && !TryGetRawgIdFromGame(item.NewData, client, true, out gameId))
                            continue;

                        SyncUserScore(item.NewData, gameId, client, token);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error syncing to RAWG library");
                PlayniteApi.Notifications.Add("rawg-sync-error", $"Error syncing to RAWG library: {ex?.Message}", NotificationType.Error);
            }
        }
        , new GlobalProgressOptions("Syncing changes to RAWG library", cancelable: true) { IsIndeterminate = false });
    }

    private void OpenSettings()
    {
        base.OpenSettingsView();
    }

    private RawgApiClient GetApiClient()
    {
        if (rawgApiClient != null)
            return rawgApiClient;

        if (string.IsNullOrWhiteSpace(settings.Settings.UserToken))
        {
            PlayniteApi.Notifications.Add(new NotificationMessage("rawg-library-no-token", "Not authenticated. Please log in in the RAWG Library extension settings. (click this notification)", NotificationType.Error, OpenSettings));
            return null;
        }

        return rawgApiClient ?? (rawgApiClient = new RawgApiClient(settings.Settings.User?.ApiKey));
    }

    public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
    {
        List<GameMetadata> output = new List<GameMetadata>();

        try
        {
            var client = GetApiClient();

            if (client == null)
                return output;

            if (settings.Settings.ImportUserLibrary)
            {
                if (string.IsNullOrWhiteSpace(settings.Settings.UserToken))
                {
                    PlayniteApi.Notifications.Add(new NotificationMessage("rawg-library-no-token", "Not authenticated. Please log in in the RAWG Library extension settings. (click this notification)", NotificationType.Error, OpenSettings));
                    return output;
                }

                var statusesToImport = settings.Settings.RawgToPlayniteStatuses?.Where(rtps => rtps.Value != RawgMapping.DoNotImportId).Select(x => x.Key).ToArray();

                var userLibrary = client.GetCurrentUserLibrary(settings.Settings.UserToken, statusesToImport);
                if (userLibrary != null)
                    output.AddRange(userLibrary.Select(g => RawgLibraryMetadataProvider.ToGameMetadata(g, logger, settings.Settings.LanguageCode, settings.Settings)));
            }

            foreach (var collectionSettings in settings.Settings.Collections)
            {
                if (!collectionSettings.Import)
                    continue;

                var collectionGames = client.GetCollectionGames(collectionSettings.Collection.Id.ToString());
                if (collectionGames != null)
                    output.AddRange(collectionGames.Select(g => RawgLibraryMetadataProvider.ToGameMetadata(g, logger, settings.Settings.LanguageCode, settings.Settings)));
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error while importing RAWG library");
            PlayniteApi.Notifications.Add(new NotificationMessage("rawg-library-error", "Error while importing RAWG library: " + ex.Message, NotificationType.Error));
        }

        return output;
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new RawgLibrarySettingsView();
    }

    public override LibraryMetadataProvider GetMetadataDownloader()
    {
        return base.GetMetadataDownloader();
    }

    public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
    {
        yield return new GameMenuItem { MenuSection = "RAWG", Description = "Sync to RAWG library", Action = SyncGamesToRawg };
        yield return new GameMenuItem { MenuSection = "RAWG", Description = "Delete from RAWG library", Action = a => DeleteGamesFromRawgLibrary(a.Games) };
        yield return new GameMenuItem { MenuSection = "RAWG", Description = "Add to new private collection", Action = a => AddToNewCollection(a.Games, isPrivate: true) };
        yield return new GameMenuItem { MenuSection = "RAWG", Description = "Add to new public collection", Action = a => AddToNewCollection(a.Games, isPrivate: false) };

        var collections = settings?.Settings?.Collections;
        if (collections == null)
            yield break;

        foreach (var collectionConfig in collections)
        {
            var collection = collectionConfig.Collection;
            yield return new GameMenuItem { MenuSection = "RAWG", Description = $"Add to collection: {collection.Name}", Action = a => AddToExistingCollection(a.Games, collection) };
        }
    }

    private void SyncGamesToRawg(GameMenuItemActionArgs args)
    {
        PlayniteApi.Dialogs.ActivateGlobalProgress(progressArgs =>
        {
            progressArgs.ProgressMaxValue = args.Games.Count;
            try
            {
                PlayniteApi.Database.Games.BeginBufferUpdate();
                foreach (var g in args.Games)
                {
                    if (progressArgs.CancelToken.IsCancellationRequested)
                        return;
                    progressArgs.CurrentProgressValue++;
                    SyncGameToRawg(g);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error syncing to RAWG");
            }
            finally
            {
                PlayniteApi.Database.Games.EndBufferUpdate();
            }
        }, new GlobalProgressOptions("Syncing games to RAWG", true) { IsIndeterminate = false });
    }

    private bool SyncCompletionStatus(Game game, int rawgId, RawgApiClient client, string token)
    {
        if (!settings.Settings.PlayniteToRawgStatuses.TryGetValue(game.CompletionStatusId, out string rawgStatus))
        {
            logger.Warn($"No RAWG status configured for Playnite completion status {game.CompletionStatus.Name}");
            return false;
        }
        else
        {
            logger.Info($"Updating RAWG game {game.Name}");
            if (client.UpdateGameCompletionStatus(token, rawgId, rawgStatus))
            {
                return true;
            }
            else
            {
                logger.Info($"Adding RAWG game {game.Name}");
                return client.AddGameToLibrary(token, rawgId, rawgStatus);
            }
        }
    }

    private void SyncUserScore(Game game, int rawgId, RawgApiClient client, string token)
    {
        if (game.UserScore.HasValue)
        {
            int userScore = game.UserScore.Value;
            foreach (var ratingsMapping in settings.Settings.PlayniteToRawgRatings)
            {
                if (ratingsMapping.Value.Min <= userScore && userScore <= ratingsMapping.Value.Max)
                {
                    logger.Info($"Rating RAWG game {game.Name}");
                    client.RateGame(token, rawgId, ratingsMapping.Key);
                    break;
                }
                logger.Warn($"No user score mapping found for Playnite user score {game.UserScore}");
            }
        }
        else
        {
            var review = client.GetCurrentUserReview(token, rawgId);
            if (review != null && string.IsNullOrWhiteSpace(review.Text)) //don't delete reviews people spent time writing
            {
                client.DeleteReview(token, review.Id);
            }
        }
    }

    private void SyncGameToRawg(Game game)
    {
        var rawgId = GetRawgIdFromGame(game);
        if (rawgId == null)
        {
            logger.Warn($"Could not find the game {game.Name} on RAWG");
            return;
        }

        string token = settings.Settings.UserToken;
        var client = GetApiClient();
        if (token == null || client == null)
            return;

        SyncCompletionStatus(game, rawgId.Value, client, token);
        SyncUserScore(game, rawgId.Value, client, token);
    }

    private bool TryGetRawgIdFromGame(Game game, RawgApiClient client, bool setLink, out int rawgId)
    {
        rawgId = GetRawgIdFromGame(game, client, setLink) ?? -1;
        return rawgId != -1;
    }

    private int? GetRawgIdFromGame(Game game, RawgApiClient client = null, bool setLink = true)
    {
        var id = RawgMetadataHelper.GetRawgIdFromGame(game);
        if (id.HasValue)
            return id;

        client = client ?? GetApiClient();
        if (client == null)
            return null;

        var searchResultGame = RawgMetadataHelper.GetExactTitleMatch(game, client, PlayniteApi, setLink);
        return searchResultGame?.Id;
    }

    public void DeleteGamesFromRawgLibrary(ICollection<Game> games)
    {
        var client = GetApiClient();
        if (client == null || games?.Any() != true)
            return;

        if (PlayniteApi.Dialogs.ShowMessage("This will delete the selected games from your online RAWG library. Are you sure?", "Deletion confirmation", System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes)
            return;

        PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
        {
            a.ProgressMaxValue = games.Count;
            try
            {
                PlayniteApi.Database.Games.BeginBufferUpdate();
                foreach (var game in games)
                {
                    if (a.CancelToken.IsCancellationRequested)
                        return;

                    a.CurrentProgressValue++;
                    var id = GetRawgIdFromGame(game);
                    if (id == null)
                        continue;

                    client.DeleteGameFromLibrary(settings.Settings.UserToken, id.Value);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error deleting games");
                PlayniteApi.Notifications.Add("rawg-delete-error", $"Error deleting games from RAWG library: {ex?.Message}", NotificationType.Error);
            }
            finally
            {
                PlayniteApi.Database.Games.EndBufferUpdate();
            }
        }, new GlobalProgressOptions("Deleting games from RAWG Library", true) { IsIndeterminate = false });

    }

    public void AddToNewCollection(List<Game> games, bool isPrivate)
    {
        var collectionNameResult = PlayniteApi.Dialogs.SelectString("Collection name:", "Create new collection", string.Empty);
        if (!collectionNameResult.Result || string.IsNullOrWhiteSpace(collectionNameResult.SelectedString))
            return;

        PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
        {
            try
            {
                a.ProgressMaxValue = games.Count + 2;
                var client = GetApiClient();
                var token = settings.Settings.UserToken;

                if (a.CancelToken.IsCancellationRequested) return;
                a.CurrentProgressValue++;

                var collection = client.CreateCollection(settings.Settings.UserToken, collectionNameResult.SelectedString, string.Empty, isPrivate);
                settings.Settings.Collections.Add(new RawgCollectionSetting { Import = false, Collection = collection });
                SavePluginSettings(settings.Settings);

                var rawgIds = new List<int>();
                foreach (var game in games)
                {
                    if (a.CancelToken.IsCancellationRequested) return;
                    a.CurrentProgressValue++;

                    if (TryGetRawgIdFromGame(game, client, true, out int rawgId))
                        rawgIds.Add(rawgId);
                }
                if (a.CancelToken.IsCancellationRequested) return;
                a.CurrentProgressValue++;

                client.AddGamesToCollection(token, collection.Slug, rawgIds);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error creating new collection");
                PlayniteApi.Notifications.Add("rawg-error-new-collection", $"Error creating new collection: {ex.Message}", NotificationType.Error);
            }
        }, new GlobalProgressOptions("Adding games to new collection " + collectionNameResult.SelectedString, cancelable: true) { IsIndeterminate = false });
    }

    public void AddToExistingCollection(List<Game> games, RawgCollection collection)
    {
        PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
        {
            try
            {
                a.ProgressMaxValue = games.Count + 1;
                var client = GetApiClient();
                var token = settings.Settings.UserToken;
                var rawgIds = new List<int>();
                foreach (var game in games)
                {
                    if (a.CancelToken.IsCancellationRequested) return;
                    a.CurrentProgressValue++;

                    if (TryGetRawgIdFromGame(game, client, true, out int rawgId))
                        rawgIds.Add(rawgId);
                }

                if (a.CancelToken.IsCancellationRequested) return;
                a.CurrentProgressValue++;

                client.AddGamesToCollection(token, collection.Slug, rawgIds);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error adding games to collection");
                PlayniteApi.Notifications.Add("rawg-error-add-to-collection", $"Error adding games to collection: {ex.Message}", NotificationType.Error);
            }
        }, new GlobalProgressOptions("Adding games to collection " + collection.Name, cancelable: true) { IsIndeterminate = false });
    }
}