using System.Collections.Generic;
using System.Linq;

namespace PlayniteExtensions.Metadata.Common;

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
