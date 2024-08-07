using GiantBombMetadata.Api;
using GiantBombMetadata.SearchProviders;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace GiantBombMetadata
{
    public class GiantBombMetadata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private IWebDownloader downloader = new WebDownloader();

        public GiantBombMetadataSettingsViewModel Settings { get; set; }

        public IPlatformUtility PlatformUtility { get; set; }
        public IGiantBombApiClient ApiClient => new GiantBombApiClient() { ApiKey = Settings.Settings.ApiKey };

        public override Guid Id { get; } = Guid.Parse("975c7dc6-efd5-41d4-b9c1-9394b3bfe9c6");

        public static List<MetadataField> Fields { get; } = new List<MetadataField>
        {
            MetadataField.Description,
            MetadataField.Tags,
            MetadataField.Platform,
            MetadataField.ReleaseDate,
            MetadataField.Name,
            MetadataField.Genres,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Series,
            MetadataField.AgeRating,
            MetadataField.Links,
            MetadataField.Icon,
            MetadataField.CoverImage,
            MetadataField.BackgroundImage,
        };

        public override List<MetadataField> SupportedFields => Fields;

        public override string Name { get; } = "Giant Bomb";

        public GiantBombMetadata(IPlayniteAPI api) : base(api)
        {
            Settings = new GiantBombMetadataSettingsViewModel(this);
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
            PlatformUtility = new PlatformUtility(api);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            if (BlockMissingApiKey())
                return null;

            var searchProvider = new GiantBombGameSearchProvider(ApiClient, Settings.Settings, PlatformUtility);
            var metadataProvider = new GiantBombMetadataProvider(searchProvider, options, PlayniteApi, PlatformUtility);
            return metadataProvider;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GiantBombMetadataSettingsView();
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem { Description = "Import Giant Bomb game property", MenuSection = "@Giant Bomb", Action = a => ImportGameProperty() };
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            if (!Settings.Settings.ShowTopPanelButton)
                yield break;

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "icon.png");
            yield return new TopPanelItem()
            {
                Icon = iconPath,
                Visible = true,
                Title = "Import Giant Bomb game property",
                Activated = ImportGameProperty
            };
        }

        public void ImportGameProperty()
        {
            if (BlockMissingApiKey())
                return;

            var searchProvider = new GiantBombGamePropertySearchProvider(ApiClient, new GiantBombScraper(downloader, PlatformUtility));
            var extra = new GiantBombBulkPropertyAssigner(PlayniteApi, Settings.Settings, searchProvider, new PlatformUtility(PlayniteApi), Settings.Settings.MaxDegreeOfParallelism);
            extra.ImportGameProperty();
        }

        private bool BlockMissingApiKey()
        {
            if (string.IsNullOrWhiteSpace(Settings.Settings.ApiKey))
            {
                var notification = new NotificationMessage("giantbomb-missing-api-key", "Missing Giant Bomb API key. Click here to add it.",
                                                           NotificationType.Error, () => OpenSettingsView());
                PlayniteApi.Notifications.Add(notification);
                return true;
            }
            return false;
        }
    }
}