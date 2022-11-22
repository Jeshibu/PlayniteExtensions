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

                    var userLibrary = client.GetCurrentUserLibrary(settings.Settings.UserToken);
                    if (userLibrary != null)
                        output.AddRange(userLibrary?.Select(g => RawgLibraryMetadataProvider.ToGameMetadata(g, logger, settings.Settings.LanguageCode, settings.Settings)));
                }

                foreach (var collectionSettings in settings.Settings.Collections)
                {
                    if (!collectionSettings.Import)
                        continue;

                    var collectionGames = client.GetCollectionGames(collectionSettings.Collection.Id.ToString());
                    if (collectionGames != null)
                        output.AddRange(collectionGames?.Select(g => RawgLibraryMetadataProvider.ToGameMetadata(g, logger, settings.Settings.LanguageCode, settings.Settings)));
                }
            }
            catch (Exception ex)
            {
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
            yield return new GameMenuItem { Description = "Sync to RAWG", Action = SyncGamesToRawg };
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

            if (!settings.Settings.PlayniteToRawgStatuses.TryGetValue(game.CompletionStatusId, out string rawgStatus))
            {
                logger.Warn($"No RAWG status configured for Playnite completion status {game.CompletionStatus.Name}");
                return;
            }
            else
            {
                logger.Info($"Updating RAWG game {game.Name}");
                if (!client.UpdateGameCompletionStatus(token, rawgId.Value, rawgStatus))
                {
                    logger.Info($"Adding RAWG game {game.Name}");
                    client.AddGameToLibrary(token, rawgId.Value, rawgStatus);
                }
            }

            if (game.UserScore.HasValue)
            {
                int userScore = game.UserScore.Value;
                foreach (var ratingsMapping in settings.Settings.PlayniteToRawgRatings)
                {
                    if (ratingsMapping.Value.Min <= userScore && userScore <= ratingsMapping.Value.Max)
                    {
                        logger.Info($"Rating RAWG game {game.Name}");
                        client.RateGame(token, rawgId.Value, ratingsMapping.Key);
                        break;
                    }
                    logger.Warn($"No user score mapping found for Playnite user score {game.UserScore}");
                }
            }
        }

        private static Regex rawgGameUrlRegex = new Regex(@"^https://rawg\.io/games/(?<id>[0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private int? GetRawgIdFromGame(Game game)
        {
            if (game.Links != null)
            {
                foreach (var link in game.Links)
                {
                    var match = rawgGameUrlRegex.Match(link.Url);
                    if (!match.Success)
                        continue;
                    int id = int.Parse(match.Groups["id"].Value);
                    return id;
                }
            }

            var client = GetApiClient();
            var searchResultGame = RawgMetadataHelper.GetExactTitleMatch(game, client);
            return searchResultGame?.Id;
        }
    }
}