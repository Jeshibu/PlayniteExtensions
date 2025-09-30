using FilterSearch.Helpers;
using FilterSearch.SearchItems;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace FilterSearch.SearchContexts;

public class GroupingSearchContext : SearchContext
{
    private IMainViewAPI MainViewApi { get; }

    public GroupingSearchContext(IMainViewAPI mainViewApi)
    {
        MainViewApi = mainViewApi;
        Label = "Sort by";
        UseAutoSearch = true;
    }

    public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args)
    {
        var groupings = EnumHelper.GetEnumValuesWithDescription<GroupableField>();
        foreach (var grouping in groupings)
            yield return new GroupingSearchItem(grouping.Value, grouping.Key, MainViewApi);
    }
}