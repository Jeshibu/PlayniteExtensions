using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace BigFishMetadata;

public class BigFishMetadataProvider : GenericMetadataProvider<BigFishSearchResultGame>
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

    private readonly BigFishMetadata plugin;

    public override List<MetadataField> AvailableFields => plugin.SupportedFields;

    protected override string ProviderName => "Big Fish Games";

    public BigFishMetadataProvider(IGameSearchProvider<BigFishSearchResultGame> searchProvider, MetadataRequestOptions options, BigFishMetadata plugin, IPlatformUtility platformUtility)
        : base(searchProvider, options, plugin.PlayniteApi, platformUtility)
    {
        this.plugin = plugin;
    }
}