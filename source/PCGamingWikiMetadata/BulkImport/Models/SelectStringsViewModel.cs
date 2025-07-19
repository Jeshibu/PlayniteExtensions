using System.Collections.Generic;
using System.Linq;

namespace PCGamingWikiBulkImport.Models;

public class SelectStringsViewModel(string propertyName, IEnumerable<SelectableStringViewModel> items)
{
    public string PropertyName { get; } = propertyName;
    public IList<SelectableStringViewModel> Items { get; } = items.ToList();
}

public class SelectableStringViewModel
{
    public string Value { get; set; }
    public string DisplayName { get; set; }
    public bool IsSelected { get; set; }
}
