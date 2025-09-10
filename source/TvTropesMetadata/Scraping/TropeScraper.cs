using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Playnite.SDK;

namespace TvTropesMetadata.Scraping;

public class TropeScraper(IWebViewFactory webViewFactory) : BaseScraper(webViewFactory)
{
    private List<string> VideogameCategoryUrlRoots = ["VideoGame", "VisualNovel"];

    private List<string> FolderLabelWhitelist =
    [
        "Game",
        "Visual Novel",
        "Action-Adventure",
        "Fighting",
        "First-Person Shooter",
        "Music/Rhythm",
        "Platform",
        "Real-Time Strategy",
        "Role-Playing",
        "RPG",
        "Roguelike",
        "Simulation",
        " Sim",
        "Stealth-Based Game",
        "Strategy",
        "Survival Horror",
        "Third-Person Shooter",
        "Turn-Based Strategy",
        "Sandbox"
    ];

    public override IEnumerable<TvTropesSearchResult> Search(string query)
    {
        var directUrlResult = GetBasicPageInfo(query);
        if (directUrlResult != null)
            return [directUrlResult];
        
        var results = GoogleSearch(query).Result.ToList();
        results.RemoveAll(sr => sr.Breadcrumbs.Count != 1 || sr.Breadcrumbs[0] != "Tropes");
        
        return results;
    }

    public ParsedTropePage GetGamesForTrope(string url)
    {
        return GetGamesForTrope(url, pageIsSubsection: false);
    }

    private ParsedTropePage GetGamesForTrope(string url, bool pageIsSubsection)
    {
        var doc = GetDocument(url);
        var output = new ParsedTropePage { Title = GetTitle(doc) };
        var articleContent = doc.QuerySelector(".article-content")?.InnerHtml;
        output.Items.AddRange(GetTropePageListElements(articleContent, getAllUnfiltered: pageIsSubsection).Select(ParseTropePageListItem));
        if (pageIsSubsection)
        {
            var breadcrumbLinks = doc.QuerySelectorAll(".entry-title .entry-breadcrumb > a[href]");
            foreach (var a in breadcrumbLinks)
            {
                var linkUrl = a.GetAttribute("href").GetAbsoluteUrl(url);
                var linkText = a.TextContent.HtmlDecode();
                if (IsVideogameUrl(linkUrl))
                    output.Items.Add(new() { Text = "", Works = [new() { Title = linkText, Urls = [linkUrl] }] });
            }
        }
        else
        {
            var subcategoryUrls = GetSubcategoryUrls(doc, url);
            foreach (var subcategoryUrl in subcategoryUrls)
            {
                var subcategoryPage = GetGamesForTrope(subcategoryUrl, pageIsSubsection: true);
                output.Items.AddRange(subcategoryPage.Items);
            }

            output.Items.RemoveAll(i => i.Works.Count == 0 || BlacklistedWords.Any(w => i.Text.Contains(w, StringComparison.InvariantCultureIgnoreCase)));
        }

        return output;
    }

    private bool IsVideogameFolderName(string folderName) => FolderLabelWhitelist.Any(l => folderName.Contains(l, StringComparison.InvariantCultureIgnoreCase));

    private bool IsVideogameUrl(string url)
    {
        var linkSegments = GetWikiPathSegments(url);
        return linkSegments.Length == 2 && VideogameCategoryUrlRoots.Contains(linkSegments[0]);
    }

    private IEnumerable<string> GetSubcategoryUrls(IHtmlDocument doc, string pageUrl)
    {
        var lastUrlSegment = GetWikiPathSegments(pageUrl).Last();
        var links = doc.QuerySelectorAll(".article-content > ul > li > a.twikilink[href]");

        bool IsSubcategoryUrl(string subcategoryUrl)
        {
            var linkSegments = GetWikiPathSegments(subcategoryUrl);
            return linkSegments.Length == 2 && linkSegments[0].Equals(lastUrlSegment, StringComparison.InvariantCultureIgnoreCase);
        }

        foreach (var a in links)
        {
            var linkUrl = a.GetAttribute("href");
            var linkText = a.TextContent.HtmlDecode();
            if (!IsSubcategoryUrl(linkUrl) || !IsVideogameFolderName(linkText))
                continue;

            yield return linkUrl.GetAbsoluteUrl(pageUrl);

            var childUrls = a.ParentElement.QuerySelectorAll("ul > li a.twikilink[href]").Select(x => x.GetAttribute("href").GetAbsoluteUrl(pageUrl)).ToArray();
            foreach (var childUrl in childUrls)
                yield return childUrl;
        }
    }

    private List<IElement> GetTropePageListElements(string content, bool getAllUnfiltered = false)
    {
        var htmlParser = new HtmlParser();
        var headerSegments = GetHeaderSegments(content).ToList();
        var output = new List<IElement>();

        void AddListElementsFromSourceString(string source) => output.AddRange(htmlParser.Parse(source).QuerySelectorAll("ul > li:has(> em, > a.twikilink)"));
        bool IsNonVideoGamesHeader(string header) => NonLettersAndNumbers.Replace(header, "").Contains("nonvideogame", StringComparison.InvariantCultureIgnoreCase);

        if (!getAllUnfiltered)
        {
            foreach (var segment in headerSegments)
            {
                var segmentHeader = segment.Item1;
                if (string.IsNullOrWhiteSpace(segmentHeader) || IsNonVideoGamesHeader(segmentHeader))
                    continue;

                var segmentContent = segment.Item2;
                if (IsVideogameFolderName(segmentHeader))
                    AddListElementsFromSourceString(segmentContent);

                var folderLabels = htmlParser.Parse(segmentContent).QuerySelectorAll(".folderlabel[onclick^=\"togglefolder(\"]");
                foreach (var folderLabel in folderLabels)
                {
                    var label = folderLabel.TextContent.HtmlDecode();
                    if (IsVideogameFolderName(label))
                        output.AddRange(folderLabel.NextElementSibling.QuerySelectorAll("ul > li:has(em)"));
                }
            }
        }

        void AddAllListElementsFromSegments(IList<Tuple<string, string>> hss)
        {
            if (hss.Count == 1)
                AddListElementsFromSourceString(hss[0].Item2);

            if (hss.Count > 1)
                foreach (var segment in hss)
                    if (!string.IsNullOrWhiteSpace(segment.Item1))
                        AddListElementsFromSourceString(segment.Item2);
        }

        if (output.Count == 0)
        {
            var filteredSegments = headerSegments.Where(hs => !string.IsNullOrWhiteSpace(hs.Item1) && !string.IsNullOrWhiteSpace(hs.Item2) && !IsNonVideoGamesHeader(hs.Item1)).ToList();
            AddAllListElementsFromSegments(filteredSegments);
            if (output.Count == 0)
                AddAllListElementsFromSegments(headerSegments);
        }

        return output;
    }

    private TropePageListItem ParseTropePageListItem(IElement element)
    {
        var output = new TropePageListItem { Text = element.InnerHtml.Split(["<ul>"], StringSplitOptions.RemoveEmptyEntries).First() };
        var liChildren = element.Children.Where(c => c.TagName == "EM" || IsVideoGameLink(c));

        var workNamesDeflated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        void AddWork(TvTropesWork work)
        {
            var deflatedName = NonLettersAndNumbers.Replace(work.Title, string.Empty);
            if (workNamesDeflated.Add(deflatedName))
                output.Works.Add(work);
        }

        foreach (var lic in liChildren)
        {
            var work = new TvTropesWork { Title = lic.TextContent.HtmlDecode() };
            AddWork(work);
            var links = lic.TagName == "A"
                ? [lic]
                : lic.Children.Where(IsVideoGameLink);

            foreach (var a in links)
            {
                var url = GetAbsoluteUrl(a.GetAttribute("href"));
                work.Urls.Add(url);
                var gameUrlName = GetWikiPathSegments(url).Last();
                AddWork(new TvTropesWork { Title = ReverseEngineerGameNameFromUrlSegment(gameUrlName), Urls = [url] });
            }
        }

        return output;
    }

    private readonly Regex NonLettersAndNumbers = new(@"[^\p{L}0-9]", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private bool IsVideoGameLink(IElement element)
    {
        return element.TagName == "A" && UrlBelongsToWhitelistedWorkCategory(element.GetAttribute("href"));
    }

    private string ReverseEngineerGameNameFromUrlSegment(string urlSegment)
    {
        var str = urlSegment.ToList();
        for (int i = 0; (i + 1) < str.Count; i++)
        {
            char a = str[i];
            char b = str[i + 1];
            char? c = str.Count > i + 2 ? str[i + 2] : null;

            bool upperAfterLower = char.IsUpper(b) && !char.IsUpper(a);
            bool digitAfterNonDigit = char.IsDigit(b) && !char.IsDigit(a);
            bool startOfWordAfterUpper = char.IsUpper(a) && char.IsUpper(b) && c.HasValue && char.IsLower(c.Value);

            if (upperAfterLower || digitAfterNonDigit || startOfWordAfterUpper)
            {
                str.Insert(i + 1, ' ');
                i++;
            }
        }

        return new string(str.ToArray());
    }
}

public class ParsedTropePage
{
    public string Title { get; set; }
    public List<TropePageListItem> Items { get; set; } = [];
    public override string ToString() => Title;
}

public class TropePageListItem
{
    public string Text { get; set; }

    public List<TvTropesWork> Works { get; set; } = [];

    public override string ToString() => Text;
}

public class TvTropesWork
{
    public string Title { get; set; }
    public List<string> Urls { set; get; } = [];
    public override string ToString() => Title;
}