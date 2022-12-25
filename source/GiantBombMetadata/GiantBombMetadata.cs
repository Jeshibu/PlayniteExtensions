using GiantBombMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GiantBombMetadata
{
    public class GiantBombMetadata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public GiantBombMetadataSettingsViewModel Settings { get; set; }

        public IPlatformUtility PlatformUtility { get; set; }

        public override Guid Id { get; } = Guid.Parse("975c7dc6-efd5-41d4-b9c1-9394b3bfe9c6");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
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
        };

        public override string Name => "GiantBomb";

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
            return new GiantBombMetadataProvider(options, this, new GiantBombApiClient { ApiKey = Settings.Settings.ApiKey }, PlatformUtility);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GiantBombMetadataSettingsView();
        }
    }
}