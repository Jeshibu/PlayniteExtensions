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

namespace GamersGateLibrary
{
    public class GamersGateLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GamersGateLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("b28970a8-37b0-4461-aa33-628024643e73");

        public override string Name => "GamersGate";

        public GamersGateScraper Scraper { get; }
        public IPlatformUtility PlatformUtility { get; }

        public GamersGateLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new GamersGateLibrarySettingsViewModel(this, api);
            Properties = new LibraryPluginProperties
            {
                HasSettings = true
            };
            Scraper = new GamersGateScraper();
            PlatformUtility = new PlatformUtility(api);
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            switch (settings.Settings.ImportAction)
            {
                case OnImportAction.Prompt:
                    var result = PlayniteApi.Dialogs.ShowMessage("Import GamersGate games? This will open a browser window because you might encounter CAPTCHAs that you need to solve within 60 seconds. This prompt (or the import as a whole) can be turned off in the add-on settings. Do not close the browser window during the import.", "GamersGate import", System.Windows.MessageBoxButton.OKCancel);
                    if (result == System.Windows.MessageBoxResult.Cancel)
                        return new GameMetadata[0];
                    break;
                case OnImportAction.DoNothing:
                    return new GameMetadata[0];
                case OnImportAction.ImportWithoutPrompt:
                    break;
                default:
                    break;
            }

            var webView = new WebViewWrapper(PlayniteApi);
            try
            {
                Scraper.SetWebRequestDelay(settings.Settings.MinimumWebRequestDelay, settings.Settings.MaximumWebRequestDelay);
                var data = Scraper.GetAllGames(webView).ToList();
                var output = new List<GameMetadata>(data.Count);
                foreach (var g in data)
                {
                    if (!settings.Settings.InstallData.TryGetValue(g.Id, out var installInfo))
                    {
                        installInfo = new GameInstallInfo
                        {
                            Id = g.Id,
                            OrderId = g.OrderId,
                            Name = g.Title,
                        };
                        settings.Settings.InstallData.Add(g.Id, installInfo);
                    }
                    installInfo.DownloadUrls = g.DownloadUrls;
                    installInfo.UnrevealedKey = g.UnrevealedKey;
                    installInfo.Key = g.Key;
                    installInfo.DRM = g.DRM;

                    var metadata = new GameMetadata
                    {
                        GameId = g.Id,
                        Source = new MetadataNameProperty("GamersGate"),
                    };
                    metadata.Platforms = new HashSet<MetadataProperty>(PlatformUtility.GetPlatformsFromName(g.Title, out string name));
                    metadata.Name = name;

                    if (metadata.Platforms.Count == 0)
                        metadata.Platforms.Add(new MetadataSpecProperty("pc_windows"));

                    if (settings.Settings.UseCoverImages && !string.IsNullOrWhiteSpace(g.CoverImageUrl))
                        metadata.CoverImage = new MetadataFile(g.CoverImageUrl);

                    output.Add(metadata);
                }
                return output;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching GamersGate games");
                PlayniteApi.Notifications.Add("gamersgate-error", "Error fetching GamersGate games: " + ex.Message, NotificationType.Error);
                return new GameMetadata[0];
            }
            finally
            {
                webView.Dispose();
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GamersGateLibrarySettingsView();
        }

        public override IEnumerable<InstallController> GetInstallActions(GetInstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            yield return new GamersGateManualInstallController(args.Game, settings.Settings, PlayniteApi, this);
        }

        public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            yield return new GamersGateManualUninstallController(args.Game, settings.Settings, PlayniteApi, this);
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            if (!settings.Settings.InstallData.TryGetValue(args.Game.GameId, out var installData))
            {
                logger.Debug($"No install data found for {args.Game.Name}, ID: {args.Game.GameId}");
                PlayniteApi.Dialogs.ShowErrorMessage("No install data found.", "GamersGate game launch error");
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