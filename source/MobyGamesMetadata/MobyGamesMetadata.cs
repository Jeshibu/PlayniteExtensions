using MobyGamesMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var apiClient = new MobyGamesApiClient(platformUtility) { ApiKey = "moby_GzOVPRacItjN9bYIN69NW79Wbjw" };
            var scraper = new MobyGamesScraper(platformUtility, downloader);
            var aggr = new AggregateMobyDataCollector(apiClient, scraper, settings.Settings);
            return new MobyGamesMetadataProvider(options, this, aggr);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new MobyGamesMetadataSettingsView();
        }
    }
}