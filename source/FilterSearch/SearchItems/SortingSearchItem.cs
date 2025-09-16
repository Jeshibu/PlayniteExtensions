using FilterSearch.Helpers;
using FilterSearch.SearchItems.Base;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace FilterSearch.SearchItems;

public class SortingSearchItem : BaseFilterSearchItem
{
    private SortOrder SortOrder { get; }
    private static Dictionary<SortOrderDirection, string> _sortDirections;
    private static Dictionary<SortOrderDirection, string> SortDirections => _sortDirections ??= EnumHelper.GetEnumValuesWithDescription<SortOrderDirection>();
    
    public SortingSearchItem(string name, SortOrder sortOrder, IMainViewAPI mainViewApi)
        : base(name, "Sort by", mainViewApi)
    {
        SortOrder = sortOrder;
        PrimaryAction = GetAction(SortOrderDirection.Ascending);
        SecondaryAction = GetAction(SortOrderDirection.Descending);
    }

    private SearchItemAction GetAction(SortOrderDirection direction)
    {
        return new(SortDirections[direction], () =>
        {
            var fp = MainView.GetFilterPreset();
            fp.SortingOrder = SortOrder;
            fp.SortingOrderDirection = direction;
            MainView.ApplyFilterPreset(fp);
            ShowLibraryView();
        });
    }
}