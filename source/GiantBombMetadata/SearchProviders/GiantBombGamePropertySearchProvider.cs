using GiantBombMetadata.Api;
using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
                var result = apiClient.GetGameProperty(
                    $"{searchResult.ResourceType}/{searchResult.Guid}",
                    progressArgs?.CancelToken ?? new CancellationToken());

                return result?.Games.Select(g => new GameDetails { Names = new List<string> { g.Name }, Url = g.SiteDetailUrl }) ?? new GameDetails[0];
            }
        }

        public IEnumerable<GiantBombSearchResultItem> Search(string query, CancellationToken cancellationToken = default)
        {
            var result = apiClient.SearchGameProperties(query, cancellationToken);
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