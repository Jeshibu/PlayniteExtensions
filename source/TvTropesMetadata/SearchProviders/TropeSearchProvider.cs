using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TvTropesMetadata.Scraping;

namespace TvTropesMetadata.SearchProviders;

public class TropeSearchProvider(TropeScraper scraper, TvTropesMetadataSettings settings) : IBulkPropertyImportDataSource<TvTropesSearchResult>
{
    public IEnumerable<GameDetails> GetDetails(TvTropesSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null) => GetDetails(searchResult.Url);

    public IEnumerable<GameDetails> GetDetails(string searchResultUrl)
    {
        var page = scraper.GetGamesForTrope(searchResultUrl);
        var worksByName = new Dictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var item in page.Items)
        {
            var works = settings.OnlyFirstGamePerTropeListItem ? item.Works.Take(1) : item.Works;
            foreach (var work in works)
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

        var titleComparer = new TitleComparer();

        foreach (var kvp in worksByName)
        {
            var gd = new GameDetails { Names = [kvp.Key], Url = kvp.Value.FirstOrDefault() };

            var extraName = scraper.ReverseEngineerGameNameFromUrl(gd.Url);
            if (!string.IsNullOrWhiteSpace(extraName) && !gd.Names.Contains(extraName, titleComparer))
                gd.Names.Add(extraName);

            yield return gd;
        }
    }

    public IEnumerable<TvTropesSearchResult> Search(string query, CancellationToken cancellationToken = default)
    {
        return scraper.Search(query);
    }

    public GenericItemOption<TvTropesSearchResult> ToGenericItemOption(TvTropesSearchResult item) => item.ToGenericItemOption();
}
