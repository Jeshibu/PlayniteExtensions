using FilterSearch.Helpers;
using FilterSearch.SearchItems;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace FilterSearch.SearchContexts;

public class SortingSearchContext : SearchContext
{
    private IMainViewAPI MainViewApi { get; }

    public SortingSearchContext(IMainViewAPI mainViewApi)
    {
        MainViewApi = mainViewApi;
        Label = "Sort by";
        UseAutoSearch = true;
    }

    public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args)
    {
        var sortOrders = EnumHelper.GetEnumValuesWithDescription<SortOrder>();
        foreach (var sortOrder in sortOrders)
            yield return new SortingSearchItem(sortOrder.Value, sortOrder.Key, MainViewApi);
    }
}