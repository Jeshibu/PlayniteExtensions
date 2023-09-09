using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MobyGamesMetadata.Api
{
    public class MobyGamesGroupSearchProvider : BaseAggregateMobyGamesDataCollector, ISearchableDataSourceWithDetails<SearchResult, IEnumerable<GameDetails>>
    {
        public MobyGamesGroupSearchProvider(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
            : base(apiClient, scraper, settings, platformUtility) { }

        public IEnumerable<GameDetails> GetDetails(SearchResult searchResult, GlobalProgressActionArgs progressArgs = null)
        {
            var details = new List<GameDetails>();
            return apiClient.GetAllGamesForGroup(searchResult.Id, progressArgs).Select(ToGameDetails);
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
}
