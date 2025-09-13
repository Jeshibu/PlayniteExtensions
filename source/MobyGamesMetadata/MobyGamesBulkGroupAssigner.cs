using Barnite.Scrapers;
using MobyGamesMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;

namespace MobyGamesMetadata;

public class MobyGamesBulkGroupAssigner(IPlayniteAPI playniteApi, ISearchableDataSourceWithDetails<SearchResult, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism)
    : BulkGamePropertyAssigner<SearchResult, GamePropertyImportViewModel>(playniteApi, dataSource, platformUtility, new MobyGamesIdUtility(), ExternalDatabase.MobyGames, maxDegreeOfParallelism)
{
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
        return new() { ImportTarget = MobyGamesHelper.GetGroupImportTarget(searchItem.Name, out propName) };
    }

    protected override IEnumerable<PotentialLink> GetPotentialLinks(SearchResult searchItem) => [new(MetadataProviderName, game => game.Url, IsAlreadyLinked)];

    private bool IsAlreadyLinked(IEnumerable<Link> links, string url)
    {
        var dbId = DatabaseIdUtility.GetIdFromUrl(url);
        
        return links?.Any(l => DatabaseIdUtility.GetIdFromUrl(l.Url) == dbId) ?? false;
    }
}