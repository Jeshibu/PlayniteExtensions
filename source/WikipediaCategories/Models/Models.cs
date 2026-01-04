using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace WikipediaCategories;

public class WikipediaSearchResult:IHasName
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
}

public class WikipediaGameSearchResult(string name, string url) : IGameSearchResult
{
    public string Name { get; set; } = name;
    public string Title { get; set; } = StripParentheses(name);
    public IEnumerable<string> AlternateNames { get; set; } = [];
    public IEnumerable<string> Platforms { get; set; } = [];
    public ReleaseDate? ReleaseDate { get; set; }
    public string Url { get; set; } = url;

    public static string StripParentheses(string str) => str?.Split('(')[0].Trim();
}

public enum WikipediaNamespace
{
    Article = 0,
    Category = 14,
}
