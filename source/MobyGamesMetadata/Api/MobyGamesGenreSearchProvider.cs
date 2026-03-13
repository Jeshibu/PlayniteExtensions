using MobyGamesMetadata.Api.V2;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MobyGamesMetadata.Api;

public class MobyGamesGenreSearchProvider(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
    : BaseAggregateMobyGamesDataCollector(apiClient, scraper, settings, platformUtility), IBulkPropertyImportDataSource<MobyGamesGenreSetting>
{
    public IEnumerable<GameDetails> GetDetails(MobyGamesGenreSetting searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        var result = ApiClient.GetAllGamesForGenre(searchResult.Id, progressArgs);
        return result.Select(g => ToGameDetails(g, searchGame));
    }

    public IEnumerable<MobyGamesGenreSetting> Search(string query, CancellationToken cancellationToken = default)
    {
        return Settings.Genres.Where(g => $"{g.Category} {g.Name}".Contains(query, StringComparison.InvariantCultureIgnoreCase));
    }

    public GenericItemOption<MobyGamesGenreSetting> ToGenericItemOption(MobyGamesGenreSetting item) => new(item) { Name = item.Name, Description = item.Category };
}
