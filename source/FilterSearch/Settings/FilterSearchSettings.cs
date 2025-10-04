using System.Collections.Generic;
using System.ComponentModel;

namespace FilterSearch.Settings;

public class FilterSearchSettings
{
    public FilterActionType PrimaryAction { get; set; } = FilterActionType.Append;
    
    public List<SearchPropertySetting> SearchProperties { get; set; } = [];

    public bool GlobalInstallStatusItems { get; set; } = true;
    public bool GlobalFavoriteItem { get; set; } = true;
    public bool GlobalHiddenItem { get; set; } = true;
    public bool GlobalMatchAllItem { get; set; } = true;
    public bool GlobalClearFilterItem { get; set; } = true;
    public bool AddSortingSearchContext { get; set; } = true;
    public bool AddGroupingSearchContext { get; set; } = true;
}

public class SearchPropertySetting
{
    public FilterProperty Property { get; set; }
    public bool EnableSearchContext { get; set; }
    public bool AddItemsToGlobalSearch { get; set; }
}

public enum FilterActionType
{
    [Description("Append to filter")]
    Append,
    
    [Description("Filter exclusively")]
    ApplyExclusively,
}

public enum FilterProperty
{
    FilterPreset,
    Library,
    AgeRating,
    Category,
    CompletionStatus,
    Developer,
    Publisher,
    Feature,
    Genre,
    Platform,
    Region,
    Series,
    Source,
    Tag
}