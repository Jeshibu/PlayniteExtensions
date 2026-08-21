using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using Rawg.Common;
using RawgMetadata.Database;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RawgMetadata;

public class RawgTagImport(IGameDatabaseAPI playniteDatabase, BulkPropertyUserInterface ui, RawgTagImportDataSource dataSource, IPlatformUtility platformUtility, IExternalDatabaseIdUtility databaseIdUtility, int maxDegreeOfParallelism = 8)
    : BulkGamePropertyAssigner<RawgTag, GamePropertyImportViewModel>(playniteDatabase, ui, dataSource, platformUtility, databaseIdUtility, ExternalDatabase.RAWG, maxDegreeOfParallelism)
{
    public override string MetadataProviderName => "Rawg";

    protected override PropertyImportSetting GetPropertyImportSetting(RawgTag searchItem, out string name)
    {
        name = searchItem.Name;
        return new() { ImportTarget = PropertyImportTarget.Tags };
    }
}

public class RawgTagImportDataSource(RawgDatabase db, RawgApiClient rawgApi, RawgBaseSettings settings) : IBulkPropertyImportDataSource<RawgTag>
{
    private ILogger logger = LogManager.GetLogger();

    public IEnumerable<RawgTag> Search(string query, CancellationToken cancellationToken = default)
    {
        return db.SearchTags(query, 50);
    }

    public GenericItemOption<RawgTag> ToGenericItemOption(RawgTag item)
    {
        return new(item) { Name = item.Name, Description = $"{item.GamesCount} games" };
    }

    public IEnumerable<GameDetails> GetDetails(RawgTag searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        var apiResult = rawgApi.GetGamesByTag(searchResult, progressArgs);
        return apiResult.Select(g => RawgMetadataHelper.ToGameDetails(g, logger, settings));
    }
}
