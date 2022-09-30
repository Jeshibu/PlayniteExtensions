using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ViveportLibrary
{
    public class ViveportLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ViveportLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("97d85dbd-ad52-4834-bf4b-f6681f1445cc");

        // Change to something more appropriate
        public override string Name => "Viveport";

        // Implementing Client adds ability to open it via special menu in playnite.
        public override LibraryClient Client { get; } = new ViveportLibraryClient();

        private IInstalledAppsReader InstalledAppsReader { get; }

        public ViveportLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new ViveportLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = true
            };
            InstalledAppsReader = new InstalledAppsReader();
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            var installedApps = InstalledAppsReader.GetInstalledApps();

            if (installedApps == null)
            {
                PlayniteApi.Notifications.Add(new NotificationMessage("viveport-installed-app-reader-error", "Viveport couldn't read the local installed apps. Check your Viveport desktop client installation.", NotificationType.Error));
                yield break;
            }

            foreach (var app in installedApps)
            {
                yield return new GameMetadata
                {
                    GameId = app.AppId,
                    Name = app.Title,
                    CoverImage = settings.Settings.UseCovers ? new MetadataFile(app.ImageUri) : null,
                    InstallDirectory = app.Path,
                    IsInstalled = true,
                    Source = new MetadataNameProperty("Viveport"),
                };
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new ViveportLibrarySettingsView();
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            yield return new AutomaticPlayController(args.Game)
            {
                Name = "Start via Viveport",
                Path = $"vive://runapp/{args.Game.GameId}",
                WorkingDir = args.Game.InstallDirectory,
                TrackingMode = TrackingMode.Directory,
                TrackingPath = args.Game.InstallDirectory,
            };
        }
    }
}