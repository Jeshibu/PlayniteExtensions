using GiantBombMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GiantBombMetadata.SearchProviders;

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

    public IEnumerable<GameDetails> GetDetails(GiantBombSearchResultItem searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
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
        var apiResult = apiClient.SearchGameProperties(query, cancellationToken);
        var objectResult = scraper.SearchObjects(query);
        return MergeSearchResults(query, apiResult, objectResult);
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

    private IEnumerable<GiantBombSearchResultItem> MergeSearchResults(string query, params IEnumerable<GiantBombSearchResultItem>[] results)
    {
        var input = new List<GiantBombSearchResultItem>();
        foreach (var result in results)
            input.AddRange(result);

        var comparer = new TitleComparer();
        var output = new List<GiantBombSearchResultItem>();
        output.AddRange(input.Where(i => MatchesQuery(query, i, comparer)));

        foreach (var item in output)
            input.Remove(item);

        output.AddRange(input.OrderBy(i => GetDistance(query, i)));
        return output;
    }

    private static bool MatchesQuery(string query, GiantBombSearchResultItem item, TitleComparer titleComparer)
    {
        if (titleComparer.Compare(query, item.Title) == 0)
            return true;

        foreach (var alias in item.AliasesSplit)
        {
            if (titleComparer.Compare(query, alias) == 0)
                return true;
        }
        return false;
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