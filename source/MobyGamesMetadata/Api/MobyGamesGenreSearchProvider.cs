using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MobyGamesMetadata.Api
{
    public class MobyGamesGenreSearchProvider : BaseAggregateMobyGamesDataCollector, ISearchableDataSourceWithDetails<MobyGamesGenreSetting, IEnumerable<GameDetails>>
    {
        public MobyGamesGenreSearchProvider(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
            : base(apiClient, scraper, settings, platformUtility) { }

        public IEnumerable<GameDetails> GetDetails(MobyGamesGenreSetting searchResult, GlobalProgressActionArgs progressArgs = null)
        {
            var result = apiClient.GetAllGamesForGenre(searchResult.Id, progressArgs);
            return result.Select(ToGameDetails);
        }

        public IEnumerable<MobyGamesGenreSetting> Search(string query, CancellationToken cancellationToken = default)
        {
            return settings.Genres.Where(g => $"{g.Category} {g.Name}".Contains(query, StringComparison.InvariantCultureIgnoreCase));
        }

        public GenericItemOption<MobyGamesGenreSetting> ToGenericItemOption(MobyGamesGenreSetting item)
        {
            var output = new GenericItemOption<MobyGamesGenreSetting>(item);
            output.Name = item.Name;
            output.Description = item.Category;
            return output;
        }
    }
}
