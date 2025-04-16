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

namespace MobyGamesMetadata.Api
{

    public class MobyGamesGameSearchProvider : BaseAggregateMobyGamesDataCollector, IGameSearchProvider<GameSearchResult>
    {
        private readonly MobyGamesHelper helper;

        public MobyGamesGameSearchProvider(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
            : base(apiClient, scraper, settings, platformUtility)
        {
            helper = new MobyGamesHelper(platformUtility);
        }

        public GameDetails GetDetails(GameSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
        {
            return GetDetails(searchResult.Id, searchGame);
        }

        public GameDetails GetDetails(int id, Game searchGame = null)
        {
            if (settings.DataSource == DataSource.ApiAndScraping)
            {
                var scraperDetails = scraper.GetGameDetails(id);
                var apiDetails = apiClient.GetMobyGame(id);
                var output = Merge(scraperDetails, ToGameDetails(apiDetails, searchGame));
                return output;
            }
            else if (settings.DataSource.HasFlag(DataSource.Scraping))
            {
                var gameDetails = scraper.GetGameDetails(id);
                gameDetails.Description = gameDetails.Description.MakeHtmlUrlsAbsolute(gameDetails.Url);
                return gameDetails;
            }
            else if (settings.DataSource.HasFlag(DataSource.Api))
            {
                var apiDetails = apiClient.GetMobyGame(id);
                var output = ToGameDetails(apiDetails, searchGame);
                return output;
            }
            return null;
        }

        public IEnumerable<GameSearchResult> Search(string query, CancellationToken cancellationToken = default)
        {
            if (settings.DataSource.HasFlag(DataSource.Scraping))
            {
                return scraper.GetGameSearchResults(query);
            }
            else if (settings.DataSource.HasFlag(DataSource.Api))
            {
                return apiClient.SearchGames(query, cancellationToken).Select(x => ToSearchResult(x));
            }
            return new List<GameSearchResult>();
        }

        public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
        {
            gameDetails = null;
            if (game.Links == null || game.Links.Count == 0)
                return false;

            foreach (var link in game.Links)
            {
                var id = helper.GetMobyGameIdFromUrl(link.Url);
                if (id == null)
                    continue;

                gameDetails = GetDetails(id.Value, game);

                return gameDetails != null;
            }
            return false;
        }

        public GenericItemOption<GameSearchResult> ToGenericItemOption(GameSearchResult item)
        {
            var output = new GenericItemOption<GameSearchResult>(item)
            {
                Name = item.Name,
                Description = item.Description,
            };
            return output;
        }
    }
}
