using GiantBombMetadata.Api;
using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GiantBombMetadata.SearchProviders
{
    public class GiantBombGamePropertySearchProvider : ISearchableDataSourceWithDetails<GiantBombSearchResultItem, IEnumerable<GameDetails>>
    {
        private readonly IGiantBombApiClient apiClient;
        private readonly GiantBombScraper scraper;
        private readonly ILogger logger = LogManager.GetLogger();

        public GiantBombGamePropertySearchProvider(IGiantBombApiClient apiClient, GiantBombScraper scraper)
        {
            this.apiClient = apiClient;
            this.scraper = scraper;
        }

        public IEnumerable<GameDetails> GetDetails(GiantBombSearchResultItem searchResult, GlobalProgressActionArgs progressArgs = null)
        {
            if (searchResult.ResourceType == "location")
            {
                var result = scraper.GetGamesForEntity(searchResult.SiteDetailUrl, progressArgs);
                return result;
            }
            else
            {
                var result = apiClient.GetGameProperty($"{searchResult.ResourceType}/{searchResult.Guid}");
                return result?.Games.Select(g => new GameDetails { Names = new List<string> { g.Name }, Url = g.SiteDetailUrl }) ?? new GameDetails[0];
            }
        }

        public IEnumerable<GiantBombSearchResultItem> Search(string query)
        {
            var result = apiClient.SearchGameProperties(query);
            return result;
        }

        public GenericItemOption<GiantBombSearchResultItem> ToGenericItemOption(GiantBombSearchResultItem item)
        {
            var output = new GenericItemOption<GiantBombSearchResultItem>(item);
            output.Name = item.Name;
            output.Description = item.ResourceType.ToUpper();
            if (!string.IsNullOrWhiteSpace(item.Deck))
                output.Description += Environment.NewLine + item.Deck;
            return output;
        }
    }
}