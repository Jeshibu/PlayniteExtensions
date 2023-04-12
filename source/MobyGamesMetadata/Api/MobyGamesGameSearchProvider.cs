using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobyGamesMetadata.Api
{

    public class MobyGamesGameSearchProvider : BaseAggregateMobyGamesDataCollector, IGameSearchProvider<GameSearchResult>
    {
        public MobyGamesGameSearchProvider(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
            : base(apiClient, scraper, settings, platformUtility) { }

        public GameDetails GetDetails(GameSearchResult searchResult)
        {
            if (settings.DataSource == DataSource.ApiAndScraping)
            {
                var scraperDetails = scraper.GetGameDetails(searchResult.Url);
                var apiDetails = ToGameDetails(apiClient.GetMobyGame(searchResult.Id));
                return Merge(scraperDetails, apiDetails);
            }
            else if (settings.DataSource.HasFlag(DataSource.Scraping))
            {
                return scraper.GetGameDetails(searchResult.Url);
            }
            else if (settings.DataSource.HasFlag(DataSource.Api))
            {
                return ToGameDetails(apiClient.GetMobyGame(searchResult.Id));
            }
            return null;
        }

        public IEnumerable<GameSearchResult> Search(string query)
        {
            if (settings.DataSource.HasFlag(DataSource.Scraping))
            {
                return scraper.GetGameSearchResults(query);
            }
            else if (settings.DataSource.HasFlag(DataSource.Api))
            {
                return apiClient.SearchGames(query).Select(x => ToSearchResult(x));
            }
            return new List<GameSearchResult>();
        }

        public bool TryGetDetails(Game game, out GameDetails gameDetails)
        {
            gameDetails = null;
            if (game.Links == null || game.Links.Count == 0)
                return false;

            foreach (var link in game.Links)
            {
                var id = MobyGamesHelper.GetMobyGameIdFromUrl(link.Url);
                if (id == null)
                    continue;

                if (settings.DataSource.HasFlag(DataSource.Scraping))
                    gameDetails = scraper.GetGameDetails(link.Url);
                else if (settings.DataSource.HasFlag(DataSource.Api))
                    gameDetails = ToGameDetails(apiClient.GetMobyGame(id.Value));

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
