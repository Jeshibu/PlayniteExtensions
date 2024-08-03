using MutualGames.Clients;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace MutualGames
{
    public class MutualGames : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private MutualGamesSettingsViewModel _settings;
        private IWebDownloader _downloader;

        private MutualGamesSettingsViewModel Settings { get => _settings ?? (_settings = new MutualGamesSettingsViewModel(this)); set => _settings = value; }
        private IWebDownloader Downloader => _downloader ?? (_downloader = new WebDownloader());

        public override Guid Id { get; } = Guid.Parse("c615a8d1-c262-430a-b74b-6302d3328466");

        public string Name { get; } = "Mutual Games";

        public MutualGames(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem { Description = "Import Mutual Games", Action = Import, MenuSection = $"@{Name}" };
        }

        private void Import(MainMenuItemActionArgs args)
        {
            var importer = new MutualGamesImporter(PlayniteApi, Settings.Settings, GetClients());
            importer.Import();
        }

        public override ISettings GetSettings(bool firstRunSettings) => Settings;

        public override UserControl GetSettingsView(bool firstRunSettings) => new MutualGamesSettingsView();

        public IEnumerable<IFriendsGamesClient> GetClients()
        {
            var webView = new OffScreenWebViewWrapper(PlayniteApi);
            yield return new EaClient(webView, Downloader);
            yield return new GogClient(webView);
            yield return new SteamClient(webView);
        }
    }
}