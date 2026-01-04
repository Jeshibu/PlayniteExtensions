using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace WikipediaCategories;

public class WikipediaCategoryMetadataProvider(IGameSearchProvider<WikipediaGameSearchResult> dataSource, MetadataRequestOptions options, IPlayniteAPI playniteApi, IPlatformUtility platformUtility)
    : GenericMetadataProvider<WikipediaGameSearchResult>(dataSource, options, playniteApi, platformUtility)
{
    public override List<MetadataField> AvailableFields { get; } = [MetadataField.Tags];
    protected override string ProviderName => "Wikipedia Categories";
}
