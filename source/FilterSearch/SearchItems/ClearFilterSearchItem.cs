using FilterSearch.Helpers;
using FilterSearch.SearchItems.Base;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace FilterSearch.SearchItems;

public class ClearFilterSearchItem : BaseFilterSearchItem
{
    public ClearFilterSearchItem(IMainViewAPI mainViewApi) : base("Clear filters", "Filter setting", mainViewApi)
    {
        PrimaryAction = new("Apply", ClearFilter);
        SecondaryAction = new("Clear grouping too", ClearFilterAndGrouping);
    }

    private void ClearFilter() => ClearFilter(false);
    private void ClearFilterAndGrouping() => ClearFilter(true);

    private void ClearFilter(bool clearGrouping)
    {
        var fp = MainView.GetFilterPreset(new());
        if (clearGrouping)
            fp.GroupingOrder = GroupableField.None;
        
        MainView.ApplyFilterPreset(fp);
        
        ShowLibraryView();
    }
}
