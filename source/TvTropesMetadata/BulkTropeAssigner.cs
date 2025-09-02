using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using TvTropesMetadata.Scraping;

namespace TvTropesMetadata;

public class BulkTropeAssigner(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<TvTropesSearchResult, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, TvTropesMetadataSettings settings) : BulkGamePropertyAssigner<TvTropesSearchResult, GamePropertyImportViewModel>(playniteAPI, dataSource, platformUtility, new TvTropesIdUtility(), ExternalDatabase.TvTropes, settings.MaxDegreeOfParallelism)
{
    public override string MetadataProviderName => "TV Tropes";

    protected override string GetGameIdFromUrl(string url)
    {
        var dbId = DatabaseIdUtility.GetIdFromUrl(url);
        if (dbId.Database == ExternalDatabase.TvTropes)
            return dbId.Id;

        return null;
    }

    protected override PropertyImportSetting GetPropertyImportSetting(TvTropesSearchResult searchItem, out string name)
    {
        name = searchItem.Title;
        return new PropertyImportSetting { ImportTarget = PropertyImportTarget.Tags, Prefix = settings.TropePrefix };
    }
}
