using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace OpenCriticMetadata
{
    public class OpenCriticMetadata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private OpenCriticMetadataSettingsViewModel settings { get; set; }
        private readonly IPlatformUtility platformUtility;

        public override Guid Id { get; } = Guid.Parse("f6da90b0-3042-4fe4-8d1e-7cdcf44389f7");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Description,
            MetadataField.Genres,
            MetadataField.CriticScore,
            MetadataField.CommunityScore,
            MetadataField.CoverImage,
            MetadataField.BackgroundImage,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Links,
        };

        public override string Name => "OpenCritic";

        public OpenCriticMetadata(IPlayniteAPI api) : base(api)
        {
            settings = new OpenCriticMetadataSettingsViewModel(this);
            Properties = new MetadataPluginProperties
            {
                HasSettings = false
            };
            platformUtility = new PlatformUtility(api);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new OpenCriticMetadataProvider(options, this, new OpenCriticSearchProvider(platformUtility), platformUtility);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new OpenCriticMetadataSettingsView();
        }
    }
}