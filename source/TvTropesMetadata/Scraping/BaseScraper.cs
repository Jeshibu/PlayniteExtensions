using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace TvTropesMetadata.Scraping;

public abstract class BaseScraper(IWebViewFactory webViewFactory)
{
    protected IWebView WebView => field ??= webViewFactory.CreateOffscreenView();
    protected readonly ILogger logger = LogManager.GetLogger();
    protected List<string> CategoryWhitelist = ["VideoGame", "VisualNovel"];
    protected const string articleBaseUrl = "https://tvtropes.org/pmwiki/pmwiki.php/";

    protected List<string> BlacklistedWords =
    [
        "deconstructed", "averted", "inverted", "subverted",
        "deconstructs", "averts", "inverts", "subverts"
    ];

    public static string GetGoogleSearchUrl(string query)
    {
        var escapedQuery = HttpUtility.UrlEncode($"site:tvtropes.org {query}");
        return $"https://www.google.com/search?hl=en&q={escapedQuery}";
    }

    protected async Task<IEnumerable<TvTropesSearchResult>> GoogleSearch(string query)
    {
        var url = GetGoogleSearchUrl(query);

        WebView.NavigateAndWait(url);
        if (WebView.GetCurrentAddress().StartsWith("https://consent.google.com", StringComparison.OrdinalIgnoreCase))
        {
            // This rejects Google's consent form for cookies
            await WebView.EvaluateScriptAsync("document.getElementsByTagName('form')[0].submit();");
            await Task.Delay(3000);
            WebView.NavigateAndWait(url);
        }

        var pageSource = await WebView.GetPageSourceAsync();
        var doc = await new HtmlParser().ParseAsync(pageSource);
        var resultElements = doc.QuerySelectorAll("#search [lang=en]").ToList();
        var output = new List<TvTropesSearchResult>();
        foreach (var result in resultElements)
        {
            var a = result.QuerySelector("a[href]");
            var h3 = a?.QuerySelector("h3");
            var breadCrumbElement = a?.QuerySelector("cite > span");
            var lastSpan = result.QuerySelectorAll("span")?.LastOrDefault();
            if (a == null || h3 == null)
                continue;

            var name = h3.TextContent.HtmlDecode().TrimEnd([" (trope)", " (Video Game)"]);
            
            output.Add(new()
            {
                Name = name,
                Title = name,
                Url = a.GetAttribute("href"),
                Breadcrumbs = GetBreadCrumbSegments(breadCrumbElement?.TextContent.HtmlDecode()),
                Description = lastSpan?.TextContent.HtmlDecode(),
            });
        }
        return output;
    }

    private static List<string> GetBreadCrumbSegments(string breadCrumbs)
    {
        if (breadCrumbs == null)
            return [];
        
        // › pmwiki › pmwiki.php › Main › E...
        return breadCrumbs.Split('›').Select(x => x.Trim())
                          .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    public abstract IEnumerable<TvTropesSearchResult> Search(string query);

    protected TvTropesSearchResult GetBasicPageInfo(string url)
    {
        if (url == null || !url.StartsWith(articleBaseUrl))
            return null;

        try
        {
            var doc = GetDocument(url);
            var title = GetTitle(doc);
            return new TvTropesSearchResult
            {
                Name = title,
                Title = title,
                Description = GetDescription(doc, textOnly: true),
                Url = url
            };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting basic page info");
            return null;
        }
    }

    protected string GetDescription(IHtmlDocument doc, bool textOnly = false)
    {
        var removeElements = doc.QuerySelectorAll(".quoteright, .acaptionright, p:empty, img.rounded");
        foreach (var r in removeElements)
            r.Remove();

        IElement openingH2;
        while ((openingH2 = doc.QuerySelector(".article-content > h2:nth-child(1)")) != null)
            openingH2.Remove();

        var articleContent = doc.QuerySelector(".article-content")?.InnerHtml.Trim();
        if (articleContent == null) return null;
        var descriptionString = GetHeaderSegments(articleContent).First().Item2;

        var descriptionDoc = new HtmlParser().Parse(descriptionString);

        descriptionDoc.MakeHtmlUrlsAbsolute("https://tvtropes.org/");

        return textOnly
            ? descriptionDoc.Body.TextContent.HtmlDecode()
            : descriptionDoc.Body.InnerHtml;
    }

    protected IEnumerable<Tuple<string, string>> GetHeaderSegments(string content)
    {
        var headerSegments = content.Trim().Split(["<h2>"], StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in headerSegments)
        {
            var headerAndContent = segment.Trim().Split(["</h2>"], StringSplitOptions.RemoveEmptyEntries);
            var segmentHeader = headerAndContent.Length == 2 ? headerAndContent[0].HtmlDecode() : string.Empty;
            var segmentContent = headerAndContent.Length == 2 ? headerAndContent[1] : segment;

            if (!string.IsNullOrWhiteSpace(segmentContent))
                yield return new(segmentHeader, segmentContent);
        }
    }

    protected string[] GetWikiPathSegments(string url)
    {
        var match = PathSplitter.Match(url);
        var segments = match.Groups["segment"].Captures.Cast<Capture>().Select(x => x.Value).ToArray();
        return segments;
    }

    private readonly Regex PathSplitter = new(@"pmwiki\.php(/(?<segment>\w+))+", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

    protected IHtmlDocument GetDocument(string url, int delay = 0)
    {
        WebView.NavigateAndWait(url);
        if (delay > 0)
            Thread.Sleep(delay);

        var pageSource = WebView.GetPageSource();
        var doc = new HtmlParser().Parse(pageSource);
        return doc;
    }

    protected static string GetTitle(IHtmlDocument document)
    {
        var titleElement = document.QuerySelector("h1.entry-title");
        if (titleElement == null)
            return null;

        titleElement = titleElement.QuerySelector("strong, span.wrapped_title") ?? titleElement;
        var removeElements = titleElement.QuerySelectorAll(".ns_parts, div.watch_rank_wrap");
        foreach (var r in removeElements)
            titleElement.RemoveChild(r);

        return titleElement.TextContent.HtmlDecode();
    }

    protected bool UrlBelongsToWhitelistedWorkCategory(string url) => CategoryWhitelist.Any(c => url.Contains(c, StringComparison.InvariantCultureIgnoreCase));

    protected static string GetAbsoluteUrl(string url) => url.GetAbsoluteUrl("https://tvtropes.org/");
}

public class TvTropesSearchResult : IHasName, IGameSearchResult
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string ImageUrl { get; set; }
    public List<string> Breadcrumbs { get; set; }

    public string Name { get; set; }

    public IEnumerable<string> AlternateNames => [];

    public IEnumerable<string> Platforms => [];

    public ReleaseDate? ReleaseDate => null;

    public GenericItemOption<TvTropesSearchResult> ToGenericItemOption()
    {
        var id = Url.TrimStart("https://tvtropes.org/pmwiki/pmwiki.php/");
        var description = $"{id} | {Description}";

        return new(this) { Description = description, Name = Name };
    }
}