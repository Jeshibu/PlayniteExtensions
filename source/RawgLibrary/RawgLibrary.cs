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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace RawgLibrary
{
    public class RawgLibrary : LibraryPlugin
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private static readonly string iconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.jpg");
        public override string LibraryIcon { get; } = iconPath;

        private RawgLibrarySettingsViewModel settings { get; set; }

        private RawgApiClient rawgApiClient = null;

        public override Guid Id { get; } = Guid.Parse("e894b739-2d6e-41ee-aed4-2ea898e29803");

        public override string Name { get; } = "RAWG";

        public IWebDownloader Downloader { get; } = new WebDownloader();


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
            if (!settings.Settings.AutoSyncNewGames && !settings.Settings.AutoSyncDeletedGames)
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
                //var addedItems = e.AddedItems ?? new List<Game>();
                var removedItems = e.RemovedItems ?? new List<Game>();
                a.ProgressMaxValue = /*addedItems.Count +*/ removedItems.Count;
                try
                {
                    //if (settings.Settings.AutoSyncNewGames)
                    //{
                    //    foreach (var game in addedItems)
                    //    {
                    //        if (a.CancelToken.IsCancellationRequested)
                    //            break;

                    //        a.CurrentProgressValue++;
                    //        if (game.PluginId == Id) //skip imports from RAWG library
                    //            continue;
                    //        if (game.PluginId == Guid.Empty && game.Name == "New Game") //newly added custom games won't have relevant data yet
                    //            continue;

                    //        var gameId = GetRawgIdFromGame(game, client, setLink: false); //TODO: once collection merging is in, set setLink to true again
                    //        if (gameId == null)
                    //            continue;

                    //        if (SyncCompletionStatus(game, gameId.Value, client, token))
                    //            logger.Info($"Synced {game.Name} (RAWG id {gameId}) to status {game.CompletionStatus?.Name}");
                    //        else
                    //            logger.Info($"Could not sync {game.Name} (RAWG id {gameId}) to status {game.CompletionStatus?.Name}");
                    //    }
                    //}

                    if (settings.Settings.AutoSyncDeletedGames)
                    {
                        foreach (var game in removedItems)
                        {
                            if (a.CancelToken.IsCancellationRequested)
                                break;

                            a.CurrentProgressValue++;

                            var gameId = GetRawgIdFromGame(game, client, setLink: false);
                            if (gameId == null)
                                continue;

                            if (client.DeleteGameFromLibrary(token, gameId.Value))
                                logger.Info($"Deleted {game.Name} from RAWG library (RAWG id {gameId})");
                            else
                                logger.Info($"Could not delete {game.Name} from RAWG library (RAWG id {gameId})");
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

                        if (settings.Settings.AutoSyncCompletionStatus && item.OldData.CompletionStatusId != item.NewData.CompletionStatusId)
                        {
                            if (gameId == -1 && !TryGetRawgIdFromGame(item.NewData, client, true, out gameId))
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

                        if (settings.Settings.AutoSyncNewGames
                            && item.OldData.PluginId == Guid.Empty
                            && item.OldData.Name == "New Game"
                            && item.OldData.Name != item.NewData.Name)
                        {
                            if (gameId == -1 && !TryGetRawgIdFromGame(item.NewData, client, true, out gameId))
                                continue;
                            
                            if (SyncCompletionStatus(item.NewData, gameId, client, token))
                                logger.Info($"Synced {item.NewData.Name} (RAWG id {gameId}) to status {item.NewData.CompletionStatus?.Name}");
                            else
                                logger.Info($"Could not sync {item.NewData.Name} (RAWG id {gameId}) to status {item.NewData.CompletionStatus?.Name}");
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

        private static Regex rawgGameUrlRegex = new Regex(@"^https://rawg\.io/games/(?<id>[0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private int? GetRawgIdFromGameLinks(Game game)
        {
            if (game.PluginId == Id && int.TryParse(game.GameId, out int rawgId))
                return rawgId;

            if (game.Links == null)
                return null;

            foreach (var link in game.Links)
            {
                var match = rawgGameUrlRegex.Match(link.Url);
                if (!match.Success)
                    continue;
                int id = int.Parse(match.Groups["id"].Value);
                return id;
            }
            return null;
        }

        private bool TryGetRawgIdFromGame(Game game, RawgApiClient client, bool setLink, out int rawgId)
        {
            rawgId = GetRawgIdFromGame(game, client, setLink) ?? -1;
            return rawgId != -1;
        }

        private int? GetRawgIdFromGame(Game game, RawgApiClient client = null, bool setLink = true)
        {
            var id = GetRawgIdFromGameLinks(game);
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
    }
}