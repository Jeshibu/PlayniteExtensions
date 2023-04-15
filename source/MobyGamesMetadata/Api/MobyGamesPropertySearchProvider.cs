using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MobyGamesMetadata.Api
{
    public class MobyGamesPropertySearchProvider : BaseAggregateMobyGamesDataCollector, ISearchableDataSourceWithDetails<SearchResult, IEnumerable<GameDetails>>
    {
        public MobyGamesPropertySearchProvider(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
            : base(apiClient, scraper, settings, platformUtility) { }

        public IEnumerable<GameDetails> GetDetails(SearchResult searchResult, GlobalProgressActionArgs progressArgs = null)
        {
            var details = new List<GameDetails>();
            return apiClient.GetAllGamesForGroup(searchResult.Id, progressArgs).Select(ToGameDetails);
        }

        IEnumerable<SearchResult> ISearchableDataSource<SearchResult>.Search(string query)
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
