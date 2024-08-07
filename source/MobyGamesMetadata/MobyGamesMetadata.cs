using MobyGamesMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace MobyGamesMetadata
{
    public class MobyGamesMetadata : MetadataPlugin
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly IWebDownloader downloader = new WebDownloader();
        private MobyGamesApiClient apiClient;
        public MobyGamesApiClient ApiClient { get { return apiClient ?? (apiClient = new MobyGamesApiClient(settings?.Settings?.ApiKey)); } }

        private MobyGamesMetadataSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("5d65902f-9bd9-44f9-b647-04f349622bb8");

        public override List<MetadataField> SupportedFields
        {
            get
            {
                var fields = new List<MetadataField>();
                if (settings.Settings.DataSource.HasFlag(DataSource.Api))
                {
                    fields.AddMissing(new[] {
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
                        MetadataField.Links,
                        MetadataField.AgeRating,
                    });
                }
                if (settings.Settings.DataSource.HasFlag(DataSource.Scraping))
                {
                    fields.AddMissing(new[] {
                        MetadataField.Name,
                        MetadataField.Description,
                        MetadataField.ReleaseDate,
                        MetadataField.Tags,
                        MetadataField.Platform,
                        MetadataField.Developers,
                        MetadataField.Publishers,
                        MetadataField.CoverImage,
                        MetadataField.CriticScore,
                        MetadataField.CommunityScore,
                        MetadataField.Series,
                    });
                }
                return fields;
            }
        }

        public override string Name { get; } = "MobyGames";

        public MobyGamesMetadata(IPlayniteAPI api) : base(api)
        {
            settings = new MobyGamesMetadataSettingsViewModel(this);
            Properties = new MetadataPluginProperties { HasSettings = true };
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            if (BlockMissingApiKey())
                return null;

            settings.Settings.DataSource = DataSource.ApiAndScraping;
            var platformUtility = new PlatformUtility(PlayniteApi);
            var scraper = new MobyGamesScraper(platformUtility, downloader);
            var aggr = new MobyGamesGameSearchProvider(ApiClient, scraper, settings.Settings, platformUtility);
            return new MobyGamesMetadataProvider(options, this, aggr, platformUtility, settings.Settings);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            apiClient = null; //reset client api key
            return new MobyGamesMetadataSettingsView();
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            if (settings.Settings.DataSource.HasFlag(DataSource.Api))
                yield return new MainMenuItem { Description = "Import MobyGames genre/group", MenuSection = "@MobyGames", Action = a => ImportGameProperty() };
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            if (!settings.Settings.ShowTopPanelButton || !settings.Settings.DataSource.HasFlag(DataSource.Api))
                yield break;

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "icon.png");
            yield return new TopPanelItem()
            {
                Icon = iconPath,
                Visible = true,
                Title = "Import MobyGames genre/group",
                Activated = ImportGameProperty
            };
        }

        public void ImportGameProperty()
        {
            if (BlockMissingApiKey())
                return;

            var genreOption = new MessageBoxOption("Genres", isDefault: true);
            var groupOption = new MessageBoxOption("Groups");
            var cancelOption = new MessageBoxOption("Cancel", isCancel: true);

            MessageBoxOption chosenOption;

            switch (settings.Settings.DataSource)
            {
                case DataSource.Api:
                    chosenOption = genreOption;
                    break;
                case DataSource.ApiAndScraping:
                    chosenOption = PlayniteApi.Dialogs.ShowMessage("Assign one of the following to all your matching games:", "Bulk property import",
                        System.Windows.MessageBoxImage.Question, new List<MessageBoxOption> { genreOption, groupOption, cancelOption });
                    break;
                default:
                    return;
            }
            if (chosenOption == null || chosenOption == cancelOption) return;

            var platformUtility = new PlatformUtility(PlayniteApi);
            var scraper = new MobyGamesScraper(platformUtility, downloader);
            if (chosenOption == groupOption)
            {
                var searchProvider = new MobyGamesGroupSearchProvider(ApiClient, scraper, settings.Settings, platformUtility);
                var extra = new MobyGamesBulkGroupAssigner(PlayniteApi, settings.Settings, searchProvider, platformUtility, settings.Settings.MaxDegreeOfParallelism);
                extra.ImportGameProperty();
            }
            else if (chosenOption == genreOption)
            {
                var searchProvider = new MobyGamesGenreSearchProvider(ApiClient, scraper, settings.Settings, platformUtility);
                var extra = new MobyGamesBulkGenreAssigner(PlayniteApi, searchProvider, platformUtility, settings.Settings.MaxDegreeOfParallelism);
                extra.ImportGameProperty();
            }
        }

        private bool BlockMissingApiKey()
        {
            if (settings.Settings.DataSource.HasFlag(DataSource.Api) && string.IsNullOrWhiteSpace(settings.Settings.ApiKey))
            {
                var notification = new NotificationMessage("mobygames-missing-api-key", "Missing MobyGames API key. Click here to add it.",
                                                           NotificationType.Error, () => OpenSettingsView());
                PlayniteApi.Notifications.Add(notification);
                return true;
            }
            return false;
        }
    }
}