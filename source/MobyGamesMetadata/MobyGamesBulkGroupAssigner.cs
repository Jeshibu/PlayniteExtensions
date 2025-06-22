using Barnite.Scrapers;
using MobyGamesMetadata.Api;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace MobyGamesMetadata;

public class MobyGamesBulkGroupAssigner : BulkGamePropertyAssigner<SearchResult, GamePropertyImportViewModel>
{
    private readonly MobyGamesMetadataSettings settings;

    public MobyGamesBulkGroupAssigner(IPlayniteAPI playniteAPI, MobyGamesMetadataSettings settings, ISearchableDataSourceWithDetails<SearchResult, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism)
        : base(playniteAPI, dataSource, platformUtility, new MobyGamesIdUtility(), ExternalDatabase.MobyGames, maxDegreeOfParallelism)
    {
        this.settings = settings;
    }

    public override string MetadataProviderName => "MobyGames";

    protected override string GetGameIdFromUrl(string url)
    {
        var dbId = DatabaseIdUtility.GetIdFromUrl(url);
        if (dbId.Database == ExternalDatabase.MobyGames)
            return dbId.Id;

        return null;
    }

    protected override PropertyImportSetting GetPropertyImportSetting(SearchResult searchItem, out string propName)
    {
        var importTarget = MobyGamesHelper.GetGroupImportTarget(searchItem.Name, out propName);

        return new PropertyImportSetting { ImportTarget = importTarget };
    }
}