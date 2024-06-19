using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using TvTropesMetadata.Scraping;

namespace TvTropesMetadata
{
    public class TvTropesMetadataProvider : GenericMetadataProvider<TvTropesSearchResult>
    {
        public static List<MetadataField> Fields = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Description,
            MetadataField.CoverImage,
            MetadataField.Tags,
            MetadataField.Series,
        };

        public TvTropesMetadataProvider(IGameSearchProvider<TvTropesSearchResult> dataSource, MetadataRequestOptions options, IPlayniteAPI playniteApi, IPlatformUtility platformUtility)
            : base(dataSource, options, playniteApi, platformUtility)
        {
        }

        public override List<MetadataField> AvailableFields => Fields;

        protected override string ProviderName { get; } = "TV Tropes";
    }
}