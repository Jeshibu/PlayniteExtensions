using System.Collections.Generic;
using System.Linq;

namespace PCGamingWikiBulkImport.Models;

public class SelectStringsViewModel
{
    public SelectStringsViewModel(string propertyName, IEnumerable<SelectableStringViewModel> items)
    {
        PropertyName = propertyName;
        Items = items.ToList();
    }

    public string PropertyName { get; }
    public IList<SelectableStringViewModel> Items { get; }
}

public class SelectableStringViewModel
{
    public string Value { get; set; }
    public string DisplayName { get; set; }
    public bool IsSelected { get; set; }
}
