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

public abstract class BaseScraper(IWebDownloader downloader)
{
    protected readonly IWebDownloader downloader = downloader;
    protected readonly ILogger logger = LogManager.GetLogger();
    public List<string> CategoryWhitelist = ["VideoGame", "VisualNovel"];
    public List<string> BlacklistedWords =
    [
        "deconstructed", "averted", "inverted", "subverted",
        "deconstructs", "averts", "inverts", "subverts"
    ];

    public abstract IEnumerable<TvTropesSearchResult> Search(string query);

    protected IEnumerable<TvTropesSearchResult> Search(string query, string type)
    {
        var directUrlResult = GetBasicPageInfo(query);
        if (directUrlResult != null)
        {
            yield return directUrlResult;
            yield break;
        }

        string url = $"https://tvtropes.org/pmwiki/elastic_search_result.php?q={HttpUtility.UrlEncode(query)}&page_type={type}&search_type=article";
        var doc = GetDocument(url);
        var searchResults = doc.QuerySelectorAll("a.search-result[href]");
        foreach (var a in searchResults)
        {
            var absoluteUrl = a.GetAttribute("href").GetAbsoluteUrl(url);
            var imgUrl = a.QuerySelector("img[src]")?.GetAttribute("src");
            var title = a.FirstElementChild.TextContent;
            string description = null;
            var descriptionElement = a.QuerySelector("div");
            if (descriptionElement != null)
            {
                var childrenToRemove = descriptionElement.Children.Where(c => c.ClassName == "img-wrapper" || c.ClassName == "more-button");
                foreach (var child in childrenToRemove)
                    descriptionElement.RemoveChild(child);

                description = descriptionElement.TextContent.Trim();
            }
            yield return new TvTropesSearchResult
            {
                Description = description,
                ImageUrl = imgUrl,
                Name = title,
                Title = title.TrimEnd(" (VideoGame)").TrimEnd(" (VisualNovel)"),
                Url = absoluteUrl,
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
                yield return new Tuple<string, string>(segmentHeader, segmentContent);
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
        var pageSource = downloader.DownloadString(url).ResponseContent;
        var doc = new HtmlParser().Parse(pageSource);
        return doc;
    }

    protected static string GetTitle(IHtmlDocument document)
    {
        var titleElement = document.QuerySelector("h1.entry-title");
        if (titleElement == null)
            return null;

        var strong = titleElement.QuerySelector("strong");
        if (strong != null)
            strong.Remove();

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

    public string Name { get; set; }

    public IEnumerable<string> AlternateNames => Enumerable.Empty<string>();

    public IEnumerable<string> Platforms => Enumerable.Empty<string>();

    public ReleaseDate? ReleaseDate => null;

    public GenericItemOption<TvTropesSearchResult> ToGenericItemOption()
    {
        var id = Url.TrimStart("https://tvtropes.org/pmwiki/pmwiki.php/");
        var description = $"{id} | {Description}";

        return new GenericItemOption<TvTropesSearchResult>(this) { Description = description, Name = Name };
    }
}
