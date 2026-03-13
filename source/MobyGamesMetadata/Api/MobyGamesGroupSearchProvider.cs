using MobyGamesMetadata.Api.V2;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MobyGamesMetadata.Api;

public class MobyGamesGroupSearchProvider(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
    : BaseAggregateMobyGamesDataCollector(apiClient, scraper, settings, platformUtility), IBulkPropertyImportDataSource<SearchResult>
{
    public IEnumerable<GameDetails> GetDetails(SearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        if (Settings.DataSource.HasFlag(DataSource.Api))
        {
            var result = ApiClient.GetAllGamesForGroup(searchResult.Id, progressArgs);
            return result.Select(g => ToGameDetails(g, searchGame));
        }

        if (Settings.DataSource.HasFlag(DataSource.Scraping))
            return Scraper.GetGamesFromGroup(searchResult.Url, progressArgs);

        return [];
    }

    IEnumerable<SearchResult> ISearchableDataSource<SearchResult>.Search(string query, CancellationToken cancellationToken)
    {
        if (Settings.DataSource.HasFlag(DataSource.Scraping))
            return Scraper.GetGroupSearchResults(query);

        return [];
    }

    public GenericItemOption<SearchResult> ToGenericItemOption(SearchResult item) => new(item) { Name = item.Name, Description = item.Description };
}
