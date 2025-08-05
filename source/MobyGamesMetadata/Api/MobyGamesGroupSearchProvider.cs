using MobyGamesMetadata.Api.V2;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MobyGamesMetadata.Api;

public class MobyGamesGroupSearchProvider(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility) : BaseAggregateMobyGamesDataCollector(apiClient, scraper, settings, platformUtility), ISearchableDataSourceWithDetails<SearchResult, IEnumerable<GameDetails>>
{
    public IEnumerable<GameDetails> GetDetails(SearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        var result = apiClient.GetAllGamesForGroup(searchResult.Id, progressArgs);
        return result.Select(g => ToGameDetails(g, searchGame));
    }

    IEnumerable<SearchResult> ISearchableDataSource<SearchResult>.Search(string query, CancellationToken cancellationToken = default)
    {
        if (settings.DataSource.HasFlag(DataSource.Scraping))
        {
            return scraper.GetGroupSearchResults(query);
        }
        return new List<SearchResult>();
    }

    public GenericItemOption<SearchResult> ToGenericItemOption(SearchResult item)
    {
        var output = new GenericItemOption<SearchResult>(item)
        {
            Name = item.Name,
            Description = item.Description,
        };
        return output;
    }
}
