using Barnite.Scrapers;
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

public class MobyGamesGameSearchProvider(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
    : BaseAggregateMobyGamesDataCollector(apiClient, scraper, settings, platformUtility), IGameSearchProvider<GameSearchResult>, IDisposable
{
    private readonly MobyGamesHelper _helper = new(platformUtility);

    public GameDetails GetDetails(GameSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        return GetDetails(searchResult.Id, searchGame, searchResult.ApiGameResult);
    }

    public GameDetails GetDetails(int id, Game searchGame = null, MobyGame searchResultGame = null)
    {
        if (Settings.DataSource == DataSource.ApiAndScraping)
        {
            var scraperDetails = Scraper.GetGameDetails(id);
            var apiDetails = searchResultGame ?? ApiClient.GetMobyGame(id);
            var output = Merge(scraperDetails, ToGameDetails(apiDetails, searchGame));
            return output;
        }

        if (Settings.DataSource.HasFlag(DataSource.Scraping))
        {
            var gameDetails = Scraper.GetGameDetails(id);
            gameDetails.Description = gameDetails.Description.MakeHtmlUrlsAbsolute(gameDetails.Url);
            return gameDetails;
        }

        if (Settings.DataSource.HasFlag(DataSource.Api))
        {
            var apiDetails = searchResultGame ?? ApiClient.GetMobyGame(id);
            var output = ToGameDetails(apiDetails, searchGame);
            return output;
        }

        return null;
    }

    public IEnumerable<GameSearchResult> Search(string query, CancellationToken cancellationToken = default)
    {
        if (Settings.DataSource.HasFlag(DataSource.Scraping))
            return Scraper.GetGameSearchResults(query);

        if (Settings.DataSource.HasFlag(DataSource.Api))
            return ApiClient.SearchGames(query, cancellationToken).Select(ToSearchResult);

        return [];
    }

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        gameDetails = null;
        if (game.Links == null || game.Links.Count == 0)
            return false;

        foreach (var link in game.Links)
        {
            var id = _helper.GetMobyGameIdFromUrl(link.Url);
            if (id == null)
                continue;

            gameDetails = GetDetails(id.Value, game);

            return gameDetails != null;
        }

        return false;
    }

    public GenericItemOption<GameSearchResult> ToGenericItemOption(GameSearchResult item) => new(item) { Name = item.Name, Description = item.Description };

    public void Dispose()
    {
        Scraper.Dispose();
    }
}
