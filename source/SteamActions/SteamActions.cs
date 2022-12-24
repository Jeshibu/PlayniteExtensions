using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using SteamAppInfoParser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ValveKeyValue;

namespace SteamActions
{
    public class SteamActions : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SteamActionsSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("c9c98ccc-466a-4ce2-9e95-ddf6ab3e164c");
        public static Guid SteamPluginId { get; } = Guid.Parse("CB91DFC9-B977-43BF-8E70-55F46E410FAB");

        public SteamActions(IPlayniteAPI api) : base(api)
        {
            settings = new SteamActionsSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            // Add code to be executed when library is updated.
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var steamGames = args.Games.Where(g => g.PluginId == SteamPluginId).ToList();
            if (steamGames.Count == 0)
                yield break;

            if (args.Games.Count == 1 && settings.Settings.ProvideControllerConfigAction)
            {
                yield return new GameMenuItem { Description = "Configure Steam Controller Layout", Action = a => Process.Start($"steam://currentcontrollerconfig/{a.Games[0].GameId}") };
            }

            yield return new GameMenuItem { Description = "Set Steam play actions", Action = a => SetPlayActions(steamGames) };
        }

        public void SetPlayActions(ICollection<Game> games)
        {
            PlayniteApi.Dialogs.ActivateGlobalProgress(args =>
            {
                try
                {
                    PlayniteApi.Database.Games.BeginBufferUpdate();

                    args.ProgressMaxValue = games.Count * 2;
                    var gamesById = new Dictionary<uint, Game>();
                    foreach (var g in games)
                    {
                        if (!uint.TryParse(g.GameId, out uint appId))
                        {
                            logger.Warn($"Game {g.Name} has an invalid Steam ID: {g.GameId}");
                            continue;
                        }
                        if (gamesById.ContainsKey(appId))
                        {
                            logger.Warn($"Duplicate Steam appId: {appId} for {g.Name}");
                            continue;
                        }
                        gamesById.Add(appId, g);
                    }

                    var data = VdfData.GetAppInfo();
                    args.CurrentProgressValue = games.Count;
                    foreach (var appInfo in data)
                    {
                        if (!gamesById.TryGetValue(appInfo.AppID, out Game game))
                            continue;

                        args.CurrentProgressValue++;

                        var launchSection = appInfo.Data["config"]?["launch"] as KVCollectionValue;

                        if (launchSection == null)
                        {
                            logger.Warn($"No launch section found for {game.Name}");
                            continue;
                        }

                        var launchOptions = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                        foreach (var launchOption in launchSection)
                        {
                            var oslist = launchOption["config"]?["oslist"]?.ToString(null);
                            if (oslist != null && !oslist.Contains("windows"))
                            {
                                logger.Trace($"Skipping launch option because oslist is {oslist}");
                                continue;
                            }

                            var betaKey = launchOption["config"]?["BetaKey"]?.ToString(null);
                            if(betaKey != null)
                            {
                                logger.Trace($"Skipping launch option because it's in beta: {betaKey}");
                                continue;
                            }

                            var launchType = launchOption["type"]?.ToString(null);
                            var description = launchOption["description"]?.ToString(null);
                            var descriptionLoc = launchOption["description_loc"]?[settings.Settings.LanguageKey]?.ToString(null);
                            var finalDescription = descriptionLoc ?? description ?? $"Play {game.Name}";

                            if (launchOptions.ContainsKey(launchType))
                            {
                                logger.Warn($"Duplicate launch type: {launchType}");
                                continue;
                            }
                            launchOptions.Add(launchType, finalDescription);
                        }

                        if (launchOptions.Count < 2)
                        {
                            logger.Trace($"Game {game.Name} has {launchOptions.Count} launch options. Skipping setting them as play actions.");
                            continue;
                        }

                        if (game.GameActions == null)
                            game.GameActions = new ObservableCollection<GameAction>();
                        else
                            game.GameActions = new ObservableCollection<GameAction>(game.GameActions);

                        foreach (var lo in launchOptions)
                        {
                            var path = $"steam://launch/{game.GameId}/{lo.Key}";
                            if (game.GameActions.Any(a => path.Equals(a.Path, StringComparison.InvariantCultureIgnoreCase)))
                                continue;

                            game.GameActions.Add(new GameAction
                            {
                                Type = GameActionType.URL,
                                IsPlayAction = true,
                                Name = lo.Value,
                                Path = path,
                                TrackingMode = TrackingMode.Directory,
                                TrackingPath = game.InstallDirectory
                            });
                        }
                        game.IncludeLibraryPluginAction = false;
                        PlayniteApi.Database.Games.Update(game);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error setting play actions");
                    PlayniteApi.Notifications.Add(new NotificationMessage("steamactions-set-error", $"Error setting play actions: {ex.Message}", NotificationType.Error));
                }
                finally
                {
                    PlayniteApi.Database.Games.EndBufferUpdate();
                }
            }, new GlobalProgressOptions("Settings Steam play actions", cancelable: true) { IsIndeterminate = false });
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SteamActionsSettingsView();
        }
    }
}