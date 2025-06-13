using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace IgnMetadata;

public class IgnMetadataProvider : GenericMetadataProvider<IgnGame>
{
    public IgnMetadataProvider(IGameSearchProvider<IgnGame> dataSource, MetadataRequestOptions options, IPlayniteAPI playniteApi, IPlatformUtility platformUtility)
        : base(dataSource, options, playniteApi, platformUtility)
    {
    }

    public override List<MetadataField> AvailableFields => IgnMetadata.Fields;

    protected override string ProviderName => "IGN";
}