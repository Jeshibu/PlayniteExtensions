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
using System.Web;

namespace TvTropesMetadata.Scraping;

public abstract class BaseScraper(IWebViewFactory webViewFactory)
{
    private IWebView _webView;
    protected IWebView WebView => _webView ??= webViewFactory.CreateOffscreenView();
    protected readonly ILogger logger = LogManager.GetLogger();
    protected List<string> CategoryWhitelist = ["VideoGame", "VisualNovel"];

    protected List<string> BlacklistedWords =
    [
        "deconstructed", "averted", "inverted", "subverted",
        "deconstructs", "averts", "inverts", "subverts"
    ];

    public static string GetSearchUrl(string query)
    {
        return $"https://tvtropes.org/pmwiki/search_result.php#gsc.tab=0&gsc.q={HttpUtility.UrlEncode(query)}&gsc.sort=";
    }

    public abstract IEnumerable<TvTropesSearchResult> Search(string query);

    protected IEnumerable<TvTropesSearchResult> Search(string query, string type)
    {
        var directUrlResult = GetBasicPageInfo(query);
        if (directUrlResult != null)
        {
            yield return directUrlResult;
            yield break;
        }

        string url = GetSearchUrl(query);
        var doc = GetDocument(url);
        var searchResults = doc.QuerySelectorAll("div.gs-result");
        var skipBreadcrumbs = new[] { "TV Tropes", "pmwiki", "pmwiki.php" };

        foreach (var div in searchResults)
        {
            var a = div.QuerySelector("a.gs-title[href]");
            if (a == null)
                continue;

            var absoluteUrl = a.GetAttribute("href").GetAbsoluteUrl(url);
            var title = a.TextContent;
            var imgUrl = div.QuerySelector("img[src]")?.GetAttribute("src");
            var description = div.QuerySelector("div.gs-snippet")?.TextContent;
            var breadCrumbs = div.QuerySelectorAll("div.gs-visibleUrl-breadcrumb > span")
                                 .Select(x => x.TextContent.TrimStart('›', ' '))
                                 .SkipWhile(skipBreadcrumbs.Contains)
                                 .ToList();

            yield return new()
            {
                Description = description,
                ImageUrl = imgUrl,
                Name = title,
                Title = title.TrimEnd(" (Video Game)").TrimEnd(" (Visual Novel)"),
                Url = absoluteUrl,
                Breadcrumbs = breadCrumbs,
            };
        }
    }

    private TvTropesSearchResult GetBasicPageInfo(string url)
    {
        var urlBase = "https://tvtropes.org/pmwiki/pmwiki.php/";
        if (url == null || !url.StartsWith(urlBase))
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

    protected IHtmlDocument GetDocument(string url)
    {
        WebView.NavigateAndWait(url);
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