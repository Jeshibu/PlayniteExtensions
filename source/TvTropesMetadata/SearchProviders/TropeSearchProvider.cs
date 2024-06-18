using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TvTropesMetadata.Scraping;

namespace TvTropesMetadata.SearchProviders
{
    public class TropeSearchProvider : ISearchableDataSourceWithDetails<TvTropesSearchResult, IEnumerable<GameDetails>>
    {
        private readonly TropeScraper scraper;

        public TropeSearchProvider(TropeScraper scraper)
        {
            this.scraper = scraper;
        }

        public IEnumerable<GameDetails> GetDetails(TvTropesSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
        {
            var page = scraper.GetGamesForTrope(searchResult.Url);
            foreach (var item in page.Items)
                foreach (var work in item.Works)
                    yield return new GameDetails { Names = new List<string> { work.Title }, Url = work.Urls.FirstOrDefault(u => u.StartsWith("https://tvtropes.org/")) };
        }

        public IEnumerable<TvTropesSearchResult> Search(string query, CancellationToken cancellationToken = default)
        {
            return scraper.Search(query);
        }

        public GenericItemOption<TvTropesSearchResult> ToGenericItemOption(TvTropesSearchResult item) => item.ToGenericItemOption();
    }
}
