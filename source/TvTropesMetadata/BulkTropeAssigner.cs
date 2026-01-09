using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using TvTropesMetadata.Scraping;

namespace TvTropesMetadata;

public class BulkTropeAssigner(IPlayniteAPI playniteApi, IBulkPropertyImportDataSource<TvTropesSearchResult> dataSource, IPlatformUtility platformUtility, TvTropesMetadataSettings settings)
    : BulkGamePropertyAssigner<TvTropesSearchResult, GamePropertyImportViewModel>(playniteApi.Database, new(playniteApi), dataSource, platformUtility, new TvTropesIdUtility(), ExternalDatabase.TvTropes, settings.MaxDegreeOfParallelism)
{
    public override string MetadataProviderName => "TV Tropes";

    protected override PropertyImportSetting GetPropertyImportSetting(TvTropesSearchResult searchItem, out string name)
    {
        name = searchItem.Title;
        return new() { ImportTarget = PropertyImportTarget.Tags, Prefix = settings.TropePrefix };
    }
}
