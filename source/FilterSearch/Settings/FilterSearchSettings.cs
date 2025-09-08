using System.Collections.Generic;

namespace FilterSearch.Settings;

public class FilterSearchSettings : ObservableObject
{
    public FilterActionType PrimaryAction { get; set; } = FilterActionType.Append;
    
    public List<SearchPropertySetting> SearchProperties { get; set; } = [];
}

public class SearchPropertySetting
{
    public FilterProperty Property { get; set; }
    public bool EnableSearchContext { get; set; }
    public bool AddItemsToGlobalSearch { get; set; }
}

public enum FilterActionType
{
    Append,
    ApplyExclusively,
}
