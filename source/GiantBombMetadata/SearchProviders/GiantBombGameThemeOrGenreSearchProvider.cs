using GiantBombMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GiantBombMetadata.SearchProviders;

public class GiantBombGameThemeOrGenreSearchProvider(IGiantBombApiClient apiClient, GiantBombScraper scraper) : ISearchableDataSourceWithDetails<GiantBombSearchResultItem, IEnumerable<GameDetails>>
{
    private readonly ILogger logger = LogManager.GetLogger();

    public IEnumerable<GameDetails> GetDetails(GiantBombSearchResultItem searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        return scraper.GetGamesForGenreOrTheme(searchResult.ResourceType, searchResult.Id, progressArgs);
    }

    private List<GiantBombSearchResultItem> searchItems;

    private List<GiantBombSearchResultItem> GetSearchItems(CancellationToken cancellationToken)
    {
        if (searchItems != null)
            return searchItems;

        var themesResult = apiClient.GetThemes(cancellationToken);
        var genresResult = apiClient.GetGenres(cancellationToken);

        if (themesResult == null || genresResult == null || cancellationToken.IsCancellationRequested)
            return searchItems;

        return searchItems = [.. themesResult, .. genresResult];
    }

    public IEnumerable<GiantBombSearchResultItem> Search(string query, CancellationToken cancellationToken = default)
    {
        var items = GetSearchItems(cancellationToken);

        if(string.IsNullOrWhiteSpace(query))
            return items.OrderBy(x => x.Name);
        else
            return items.OrderBy(i => GetDistance(query, i));
    }

    public GenericItemOption<GiantBombSearchResultItem> ToGenericItemOption(GiantBombSearchResultItem item)
    {
        item.ResourceType = item.ApiDetailUrl.Split(['/'], StringSplitOptions.RemoveEmptyEntries).Reverse().Skip(1).First();

        var output = new GenericItemOption<GiantBombSearchResultItem>(item);
        output.Name = item.Name;
        output.Description = item.ResourceType.ToUpper();
        if (!string.IsNullOrWhiteSpace(item.Deck))
            output.Description += Environment.NewLine + item.Deck;
        return output;
    }

    private static int GetDistance(string query, GiantBombSearchResultItem item)
    {
        var distances = new List<int> { GetDistance(query, item.Name) };

        foreach (var alias in item.AliasesSplit)
            distances.Add(GetDistance(query, alias));

        return distances.Min();
    }

    private static int GetDistance(string query, string title)
    {
        return Math.Abs(Fastenshtein.AutoCompleteLevenshtein.Distance(query, title));
    }
}