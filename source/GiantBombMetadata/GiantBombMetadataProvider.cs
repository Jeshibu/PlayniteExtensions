using GiantBombMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace GiantBombMetadata;

public class GiantBombMetadataProvider(IGameSearchProvider<GiantBombSearchResultItem> dataSource, MetadataRequestOptions options, IPlayniteAPI playniteApi, IPlatformUtility platformUtility) : GenericMetadataProvider<GiantBombSearchResultItem>(dataSource, options, playniteApi, platformUtility)
{
    public override List<MetadataField> AvailableFields => [];

    protected override string ProviderName => "Giant Bomb";
}
