using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TvTropesMetadata.Scraping;

namespace TvTropesMetadata.SearchProviders;

public class TropeSearchProvider(TropeScraper scraper, TvTropesMetadataSettings settings) : ISearchableDataSourceWithDetails<TvTropesSearchResult, IEnumerable<GameDetails>>
{
    public IEnumerable<GameDetails> GetDetails(TvTropesSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        var page = scraper.GetGamesForTrope(searchResult.Url);
        var worksByName = new Dictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var item in page.Items)
        {
            var works = settings.OnlyFirstGamePerTropeListItem ? item.Works.Take(1) : item.Works;
            foreach (var work in item.Works)
            {
                if (!worksByName.TryGetValue(work.Title, out HashSet<string> urls))
                {
                    urls = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    worksByName.Add(work.Title, urls);
                }

                var url = work.Urls.FirstOrDefault(u => u.StartsWith("https://tvtropes.org/"));
                if (url != null)
                    urls.Add(url);
            }
        }

        foreach (var kvp in worksByName)
            yield return new GameDetails { Names = [kvp.Key], Url = kvp.Value.FirstOrDefault() };
    }

    public IEnumerable<TvTropesSearchResult> Search(string query, CancellationToken cancellationToken = default)
    {
        return scraper.Search(query);
    }

    public GenericItemOption<TvTropesSearchResult> ToGenericItemOption(TvTropesSearchResult item) => item.ToGenericItemOption();
}
