using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MobyGamesMetadata.Api
{
    public class AggregateMobyPropertyCollector : BaseAggregateMobyGamesDataCollector, ISearchableDataSourceWithDetails<SearchResult, IEnumerable<GameDetails>>
    {
        public AggregateMobyPropertyCollector(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
            : base(apiClient, scraper, settings, platformUtility) { }

        public IEnumerable<GameDetails> GetDetails(SearchResult searchResult)
        {
            var details = new List<GameDetails>();
            if (settings.DataSource.HasFlag(DataSource.Api))
            {
                int limit = 100, offset = 0;
                ICollection<MobyGame> result;
                do
                {
                    result = apiClient.GetGamesForGroup(searchResult.Id, limit, offset);
                    offset += limit;
                    if (result != null)
                        details.AddRange(result.Select(ToGameDetails));
                } while (result?.Count == limit);
            }
            return details;
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
