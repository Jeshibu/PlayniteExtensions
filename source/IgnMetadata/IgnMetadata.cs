using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace IgnMetadata
{
    public class IgnMetadata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private IgnMetadataSettingsViewModel settings { get; set; }
        private IPlatformUtility platformUtility;
        private IgnClient client;

        public override Guid Id { get; } = Guid.Parse("6024e3a9-de7e-4848-9101-7a2f818e7e47");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.CoverImage,
            MetadataField.Name,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Genres,
            MetadataField.Features,
            MetadataField.Series,
            MetadataField.Description,
            MetadataField.AgeRating,
            MetadataField.ReleaseDate,
            MetadataField.Platform,
        };

        public override string Name => "IGN";

        public IgnMetadata(IPlayniteAPI api) : base(api)
        {
            settings = new IgnMetadataSettingsViewModel(this);
            Properties = new MetadataPluginProperties
            {
                HasSettings = false
            };
            platformUtility = new PlatformUtility(PlayniteApi);
            client = new IgnClient(new WebDownloader() { Accept = "*/*" });
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new IgnMetadataProvider(options, this, client, platformUtility);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new IgnMetadataSettingsView();
        }
    }
}