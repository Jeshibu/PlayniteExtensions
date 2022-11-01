using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LegacyGamesLibrary
{
    public class LegacyGamesLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private LegacyGamesLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("34c3178f-6e1d-4e27-8885-99d4f031b168");

        public override string Name { get; } = "Legacy Games";

        public override LibraryClient Client
        {
            get
            {
                try
                {
                    string launcherPath = RegistryReader.GetLauncherPath();
                    if (string.IsNullOrWhiteSpace(launcherPath))
                        return null;

                    string iconPath = Path.Combine(Path.GetDirectoryName(launcherPath), "uninstallerIcon.ico");

                    return new LegacyGamesLibraryClient(launcherPath, iconPath);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to get client");
                    return null;
                }
            }
        }

        private LegacyGamesRegistryReader RegistryReader { get; }
        private AggregateMetadataGatherer MetadataGatherer { get; }

        public LegacyGamesLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new LegacyGamesLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = true,
                CanShutdownClient = false,
                HasCustomizedGameImport = false,
            };
            RegistryReader = new LegacyGamesRegistryReader(new RegistryValueProvider());
            MetadataGatherer = new AggregateMetadataGatherer(RegistryReader, new AppStateReader(), api, settings.Settings);
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            return MetadataGatherer.GetGames(args.CancelToken);
        }

        public override LibraryMetadataProvider GetMetadataDownloader()
        {
            return MetadataGatherer;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new LegacyGamesLibrarySettingsView();
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            if (!Guid.TryParse(args.Game.GameId, out var installerUUID))
            {
                logger.Debug($"Unexpected non-guid ID for {args.Game.Name}: {args.Game.GameId}");
                PlayniteApi.Dialogs.ShowErrorMessage("Faulty game ID", "Legacy Games launch error");
            }

            var installData = RegistryReader.GetGameData(Microsoft.Win32.RegistryView.Default).FirstOrDefault(d => d.InstallerUUID == installerUUID);

            if (installData == null)
            {
                logger.Debug($"No install data found for {args.Game.Name}, ID: {args.Game.GameId}");
                PlayniteApi.Dialogs.ShowErrorMessage("No install data found.", "Legacy Games launch error");
            }

            string path = Path.Combine(installData.InstDir, installData.GameExe);

            yield return new AutomaticPlayController(args.Game)
            {
                Path = path,
                WorkingDir = installData.InstDir,
                TrackingMode = TrackingMode.Default
            };
        }
    }
}