using System.Collections.Generic;

namespace WikipediaCategories.Models.API;

public class PageQuery
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
    public OtherPageDetails[] categories { get; set; } = [];
    public OtherPageDetails[] redirects { get; set; } = [];
}

public class OtherPageDetails
{
    public int ns { get; set; }
    public string title { get; set; }
}
