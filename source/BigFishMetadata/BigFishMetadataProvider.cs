using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace BigFishMetadata;

public class BigFishMetadataProvider(IGameSearchProvider<BigFishSearchResultGame> searchProvider, MetadataRequestOptions options, BigFishMetadata plugin, IPlatformUtility platformUtility) : GenericMetadataProvider<BigFishSearchResultGame>(searchProvider, options, plugin.PlayniteApi, platformUtility)
{
    public static List<MetadataField> Fields =
    [
        MetadataField.Name,
        MetadataField.Description,
        MetadataField.Genres,
        MetadataField.InstallSize,
        MetadataField.CoverImage,
        MetadataField.BackgroundImage,
        MetadataField.CommunityScore,
    ];

    public override List<MetadataField> AvailableFields => plugin.SupportedFields;

    protected override string ProviderName => "Big Fish Games";
}