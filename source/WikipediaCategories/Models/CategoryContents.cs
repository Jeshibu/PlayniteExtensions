using System.Collections.Generic;

namespace WikipediaCategories.Models;

public class CategoryContents
{
    public List<string> Subcategories { get; } = [];
    public List<string> Articles { get; } = [];
}
