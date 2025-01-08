using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;


namespace PCGamingWikiMetadata
{
    public class PCGamingWikiMetadata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private PCGamingWikiMetadataSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("c038558e-427b-4551-be4c-be7009ce5a8d");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Links,
            MetadataField.ReleaseDate,
            MetadataField.Genres,
            MetadataField.Series,
            MetadataField.Features,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.CriticScore,
            MetadataField.Tags,
        };
        public override string Name => "PCGamingWiki";

        public PCGamingWikiMetadata(IPlayniteAPI api) : base(api)
        {
            settings = new PCGamingWikiMetadataSettings(this);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new PCGamingWikiMetadataProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new PCGamingWikiMetadataSettingsView();
        }

    }
}
