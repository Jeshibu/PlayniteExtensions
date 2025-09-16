using FilterSearch.Helpers;
using FilterSearch.SearchItems.Base;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace FilterSearch.SearchItems;

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