using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobyGamesMetadata.Api
{
    public class AggregateMobyDataCollector : ISearchableDataSourceWithDetails<GameSearchResult, GameDetails>, ISearchableDataSourceWithDetails<SearchResult, IEnumerable<GameDetails>>
    {
        public AggregateMobyDataCollector(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings)
        {
            ApiClient = apiClient;
            Scraper = scraper;
            Settings = settings;
        }

        public MobyGamesApiClient ApiClient { get; }
        public MobyGamesScraper Scraper { get; }
        public MobyGamesMetadataSettings Settings { get; }

        public GameDetails GetDetails(GameSearchResult searchResult)
        {
            if (Settings.DataSource == DataSource.ApiAndScraping)
            {
                var scraperDetails = Scraper.GetGameDetails(searchResult.Url);
                var apiDetails = ApiClient.GetGameDetails(searchResult.Id);
                scraperDetails.CoverOptions = apiDetails.CoverOptions;
                scraperDetails.BackgroundOptions = apiDetails.BackgroundOptions;
                scraperDetails.Links = apiDetails.Links;

                if (apiDetails.Description != null)
                    scraperDetails.Description = apiDetails.Description;

                if (apiDetails.Genres != null)
                    scraperDetails.Genres = apiDetails.Genres;

                if (apiDetails.Tags != null)
                {
                    scraperDetails.Tags.AddRange(apiDetails.Tags);
                    scraperDetails.Tags = scraperDetails.Tags.Distinct().ToList();
                }
                return scraperDetails;
            }
            if (Settings.DataSource.HasFlag(DataSource.Scraping))
            {
                return Scraper.GetGameDetails(searchResult.Url);
            }
            else if (Settings.DataSource.HasFlag(DataSource.Api))
            {
                return ApiClient.GetGameDetails(searchResult.Id);
            }
            return null;
        }

        public IEnumerable<GameSearchResult> Search(string query)
        {
            if (Settings.DataSource.HasFlag(DataSource.Scraping))
            {
                return Scraper.GetGameSearchResults(query);
            }
            else if (Settings.DataSource.HasFlag(DataSource.Api))
            {
                return ((ISearchableDataSource<GameDetails>)ApiClient).Search(query).Select(ToSearchResult);
            }
            return new List<GameSearchResult>();
        }

        private GameSearchResult ToSearchResult(GameDetails gameDetails)
        {
            var result = new GameSearchResult();
            var title = gameDetails.Names.First();
            var altTitles = gameDetails.Names.Skip(1);
            result.SetName(title, altTitles);
            result.Description = gameDetails.Description;
            result.Platforms = gameDetails.Platforms;
            result.ReleaseDate = gameDetails.ReleaseDate;
            result.SetUrlAndId(gameDetails.Links.First().Url);
            return result;
        }

        IEnumerable<GameDetails> ISearchableDataSourceWithDetails<SearchResult, IEnumerable<GameDetails>>.GetDetails(SearchResult searchResult)
        {
            if (Settings.DataSource.HasFlag(DataSource.Api))
            {
                return ApiClient.GetAllGameDetailsForGroup(searchResult.Id);
            }
            return new List<GameDetails>();
        }

        IEnumerable<SearchResult> ISearchableDataSource<SearchResult>.Search(string query)
        {
            if (Settings.DataSource.HasFlag(DataSource.Scraping))
            {
                return Scraper.GetGroupSearchResults(query);
            }
            return new List<SearchResult>();
        }
    }
}
