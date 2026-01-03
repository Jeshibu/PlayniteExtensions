using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;

namespace WikipediaCategoryImport.BulkImport;

public class WikipediaCategoryBulkImport(IPlayniteAPI playniteApi, ISearchableDataSourceWithDetails<WikipediaSearchResult, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, IExternalDatabaseIdUtility databaseIdUtility, ExternalDatabase databaseType, int maxDegreeOfParallelism = 8)
    : BulkGamePropertyAssigner<WikipediaSearchResult,GamePropertyImportViewModel>(playniteApi, dataSource, platformUtility, databaseIdUtility, databaseType, maxDegreeOfParallelism)
{
    private WikipediaIdUtility _idUtility = new();

    public override string MetadataProviderName => "Wikipedia";

    protected override PropertyImportSetting GetPropertyImportSetting(WikipediaSearchResult searchItem, out string name)
    {
        name = searchItem?.Name?.Split([':'], 2).Last();
        return new() { ImportTarget = PropertyImportTarget.Tags };
    }

    protected override string GetGameIdFromUrl(string url)
    {
        var dbId = _idUtility.GetIdFromUrl(url);

        if (dbId.Database == ExternalDatabase.Wikipedia)
            return dbId.Id;

        return null;
    }
}
