using MobyGamesMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MobyGamesMetadata
{
    public class MobyGamesMetadata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private MobyGamesMetadataSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("5d65902f-9bd9-44f9-b647-04f349622bb8");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField> {
            MetadataField.Name,
            MetadataField.Description,
            MetadataField.ReleaseDate,
            MetadataField.Genres,
            MetadataField.Tags,
            MetadataField.Platform,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.CoverImage,
            MetadataField.BackgroundImage,
            MetadataField.CriticScore,
            MetadataField.CommunityScore,
            MetadataField.Series,
            MetadataField.Links,
        };

        public override string Name => "MobyGames";

        public MobyGamesMetadata(IPlayniteAPI api) : base(api)
        {
            settings = new MobyGamesMetadataSettingsViewModel(this);
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            settings.Settings.DataSource = DataSource.ApiAndScraping;
            var platformUtility = new PlatformUtility(PlayniteApi);
            var downloader = new WebDownloader();
            var apiClient = new MobyGamesApiClient() { ApiKey = settings.Settings.ApiKey };
            var scraper = new MobyGamesScraper(platformUtility, downloader);
            var aggr = new MobyGamesGameSearchProvider(apiClient, scraper, settings.Settings, platformUtility);
            return new MobyGamesMetadataProvider(options, this, aggr, platformUtility);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new MobyGamesMetadataSettingsView();
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            if (settings.Settings.DataSource == DataSource.ApiAndScraping)
                yield return new MainMenuItem { Description = "Import MobyGames group", MenuSection = "@MobyGames", Action = a => ImportGameProperty() };
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            if (!settings.Settings.ShowTopPanelButton || settings.Settings.DataSource != DataSource.ApiAndScraping)
                yield break;

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "icon.png");
            yield return new TopPanelItem()
            {
                Icon = iconPath,
                Visible = true,
                Title = "Import MobyGames group",
                Activated = ImportGameProperty
            };
        }

        public void ImportGameProperty()
        {
            var platformUtility = new PlatformUtility(PlayniteApi);
            var downloader = new WebDownloader();
            var apiClient = new MobyGamesApiClient() { ApiKey = settings.Settings.ApiKey };
            var scraper = new MobyGamesScraper(platformUtility, downloader);
            var searchProvider = new AggregateMobyPropertyCollector(apiClient, scraper, settings.Settings, platformUtility);
            var extra = new MobyGamesBulkPropertyAssigner(PlayniteApi, settings.Settings, searchProvider, platformUtility);
            extra.ImportGameProperty();
        }
    }
}