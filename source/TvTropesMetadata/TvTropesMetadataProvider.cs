using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using TvTropesMetadata.Scraping;

namespace TvTropesMetadata;

public class TvTropesMetadataProvider(IGameSearchProvider<TvTropesSearchResult> dataSource, MetadataRequestOptions options, IPlayniteAPI playniteApi, IPlatformUtility platformUtility) : GenericMetadataProvider<TvTropesSearchResult>(dataSource, options, playniteApi, platformUtility)
{
    public static List<MetadataField> Fields =
    [
        MetadataField.Name,
        MetadataField.Description,
        MetadataField.CoverImage,
        MetadataField.Tags,
        MetadataField.Series,
    ];

    public override List<MetadataField> AvailableFields => Fields;

    protected override string ProviderName { get; } = "TV Tropes";
}