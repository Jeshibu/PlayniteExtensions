using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GameJoltLibrary
{
    public class GameJoltLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GameJoltLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("593e5f0e-c2d7-4e54-be9d-847976d62c46");

        // Change to something more appropriate
        public override string Name => "Game Jolt";

        // Implementing Client adds ability to open it via special menu in playnite.
        //public override LibraryClient Client { get; } = new GameJoltLibraryClient();

        public IWebDownloader Downloader { get; }

        private WttfReader WttfReader { get; }

        public GameJoltLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new GameJoltLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = false
            };
            WttfReader = new WttfReader();
            Downloader = new WebDownloader();
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {

            List<GameMetadata> games;
            try
            {
                games = WttfReader.GetGameMetadata().ToList();
            }
            catch (FileNotFoundException)
            {
                PlayniteApi.Notifications.Add(new NotificationMessage("gamejolt-fnf", "GameJolt library files not found. Check GameJolt client installation.", NotificationType.Error));
                return new GameMetadata[0];
            }

            var installedDbGames = PlayniteApi.Database.Games.Where(g => g.PluginId == Id && g.IsInstalled);
            using (PlayniteApi.Database.BufferedUpdate())
            {
                foreach (var installedDbGame in installedDbGames)
                {
                    bool shouldBeMarkedInstalled = games.Any(g => g.GameId == installedDbGame.GameId);
                    if (!shouldBeMarkedInstalled)
                    {
                        installedDbGame.IsInstalled = false;
                        installedDbGame.InstallDirectory = null;
                        PlayniteApi.Database.Games.Update(installedDbGame);
                    }
                }
            }

            return games;
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            int gameId = int.Parse(args.Game.GameId);

            IEnumerable<GameAction> actions;
            try
            {
                actions = WttfReader.GetActions(gameId);
            }
            catch (FileNotFoundException)
            {
                PlayniteApi.Notifications.Add(new NotificationMessage("gamejolt-fnf", "GameJolt library files not found. Check GameJolt client installation.", NotificationType.Error));
                yield break;
            }

            foreach (var a in actions)
            {
                yield return new AutomaticPlayController(args.Game)
                {
                    Type = AutomaticPlayActionType.File,
                    WorkingDir = a.WorkingDir,
                    Path = a.Path,
                    TrackingMode = a.TrackingMode,
                    Name = a.Name,
                };
            }
        }

        //public override ISettings GetSettings(bool firstRunSettings)
        //{
        //    return settings;
        //}

        //public override UserControl GetSettingsView(bool firstRunSettings)
        //{
        //    return new GameJoltLibrarySettingsView();
        //}

    }
}