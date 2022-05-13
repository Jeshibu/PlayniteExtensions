using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GroupeesLibrary
{
    public class GroupeesLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GroupeesLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("42ff71aa-34dc-4b12-86b6-1328136c958f");

        public override string Name => "Groupees";

        public GroupeesScraper Scraper { get; }
        public IWebDownloader Downloader { get; }

        public GroupeesLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new GroupeesLibrarySettingsViewModel(this, api);
            Properties = new LibraryPluginProperties
            {
                HasSettings = true
            };
            Scraper = new GroupeesScraper();
            Downloader = new WebDownloader();
        }

        public bool IsAuthenticated(GroupeesLibrarySettings s)
        {
            s.Cookies.ForEach(Downloader.Cookies.Add);
            try
            {
                var csrfToken = Scraper.GetAuthenticatedCsrfToken(Downloader);
                return csrfToken != null;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error checking authentication status");
                PlayniteApi.Notifications.Add("groupees-error", "Error checking Groupees authentication status: " + ex.Message, NotificationType.Error);
                return false;
            }
            finally
            {
                settings.Settings.Cookies = Downloader.Cookies.Cast<Cookie>().ToList();
                SavePluginSettings(settings.Settings);
            }
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            settings.Settings.Cookies.ForEach(Downloader.Cookies.Add);
            try
            {
                var token = Scraper.GetAuthenticatedCsrfToken(Downloader);
                if (token == null)
                {
                    logger.Error("CSRF token is empty or user is not authenticated");
                    PlayniteApi.Notifications.Add("groupees-error", "Error fetching Groupees games: User not authenticated", NotificationType.Error);
                    return null;
                }

                var data = Scraper.GetGames(settings.Settings, Downloader, token).ToList();
                return data;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Groupees games");
                PlayniteApi.Notifications.Add("groupees-error", "Error fetching Groupees games: " + ex.Message, NotificationType.Error);
                return new GameMetadata[0];
            }
            finally
            {
                settings.Settings.Cookies = Downloader.Cookies.Cast<Cookie>().ToList();
                SavePluginSettings(settings.Settings);
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GroupeesLibrarySettingsView();
        }

        public override IEnumerable<InstallController> GetInstallActions(GetInstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            yield return new GroupeesManualInstallController(args.Game, settings.Settings, PlayniteApi, this);
        }

        public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            yield return new GroupeesManualUninstallController(args.Game, settings.Settings, PlayniteApi, this);
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            if (!settings.Settings.InstallData.TryGetValue(args.Game.GameId, out var installData))
            {
                logger.Debug($"No install data found for {args.Game.Name}, ID: {args.Game.GameId}");
                PlayniteApi.Dialogs.ShowErrorMessage("No install data found.", "Groupees game launch error");
            }

            string path = Path.Combine(installData.InstallLocation, installData.RelativeExecutablePath);

            yield return new AutomaticPlayController(args.Game)
            {
                Path = path,
                WorkingDir = installData.InstallLocation,
                TrackingMode = TrackingMode.Default
            };
        }
    }
}