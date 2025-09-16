using FilterSearch.Helpers;
using FilterSearch.SearchItems.Base;
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
        {
            yield return new GroupingSearchItem(grouping.Value, grouping.Key, MainViewApi);
        }
    }
}

public class GroupingSearchItem : BaseFilterSearchItem
{
    private GroupableField Grouping { get; }

    public GroupingSearchItem(string name, GroupableField grouping, IMainViewAPI mainViewApi)
        : base(name, "Group by", mainViewApi)
    {
        Grouping = grouping;
        PrimaryAction = new("Apply", ApplyGrouping);
    }

    private void ApplyGrouping()
    {
        var fp = MainView.GetFilterPreset();
        fp.GroupingOrder = Grouping;
        MainView.ApplyFilterPreset(fp);
        ShowLibraryView();
    }
}