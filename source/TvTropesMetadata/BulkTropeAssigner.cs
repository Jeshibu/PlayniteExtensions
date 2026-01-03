using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using TvTropesMetadata.Scraping;

namespace TvTropesMetadata;

public class BulkTropeAssigner(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<TvTropesSearchResult, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, TvTropesMetadataSettings settings) : BulkGamePropertyAssigner<TvTropesSearchResult, GamePropertyImportViewModel>(playniteAPI, dataSource, platformUtility, new TvTropesIdUtility(), ExternalDatabase.TvTropes, settings.MaxDegreeOfParallelism)
{
    public override string MetadataProviderName => "TV Tropes";

    protected override PropertyImportSetting GetPropertyImportSetting(TvTropesSearchResult searchItem, out string name)
    {
        name = searchItem.Title;
        return new() { ImportTarget = PropertyImportTarget.Tags, Prefix = settings.TropePrefix };
    }
}
