using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace WikipediaCategoryImport;

public class WikipediaSearchResult
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
}

public class WikipediaGameSearchResult : IGameSearchResult
{
    public string Name { get; }
    public string Title { get; }
    public IEnumerable<string> AlternateNames { get; } = [];
    public IEnumerable<string> Platforms { get; } = [];
    public ReleaseDate? ReleaseDate { get; }
    public string Url { get; }
}

public enum WikipediaNamespace
{
    Article = 0,
    Category = 14,
}

public class WikipediaArticleResponse
{
    public string batchcomplete { get; set; }
    public Dictionary<string, string> @continue { get; set; }
    public Query query { get; set; }
}

public class Query
{
    public NormalizedTitle[] normalized { get; set; } = [];
    public Redirects[] redirects { get; set; } = [];
    public Dictionary<string, PageData> pages { get; set; } = [];
}

public class NormalizedTitle
{
    public string from { get; set; }
    public string to { get; set; }
}

public class Redirects
{
    public string from { get; set; }
    public string to { get; set; }
}

public class PageData
{
    public int pageid { get; set; }
    public int ns { get; set; }
    public string title { get; set; }
    public Categories[] categories { get; set; }
}

public class Categories
{
    public int ns { get; set; }
    public string title { get; set; }
}
