using GiantBombMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace GiantBombMetadata
{
    public class GiantBombMetadataProvider : GenericMetadataProvider<GiantBombSearchResultItem>
    {
        public GiantBombMetadataProvider(IGameSearchProvider<GiantBombSearchResultItem> dataSource, MetadataRequestOptions options, IPlayniteAPI playniteApi, IPlatformUtility platformUtility)
            : base(dataSource, options, playniteApi, platformUtility) { }

        public override List<MetadataField> AvailableFields => GiantBombMetadata.Fields;

        protected override string ProviderName { get; } = "Giant Bomb";
    }
}