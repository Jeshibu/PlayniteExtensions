using System.Collections.Generic;

namespace WikipediaCategories.Models;

public class ArticleDetails
{
    public string Title { get; set; }
    public List<string> Categories { get; set; } = [];
    public List<string> Redirects { get; set; } = [];
    public string Url { get; set; }
}
