using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Playnite.SDK;
using System.Threading.Tasks;

namespace TvTropesMetadata.Scraping;

public class TropeScraper(IWebViewFactory webViewFactory) : BaseScraper(webViewFactory)
{
    private readonly List<string> _videogameCategoryUrlRoots = ["VideoGame", "VisualNovel"];

    private readonly List<string> _folderLabelContainsWhitelist =
    [
        "Game",
        "Visual Novel",
        "Action-Adventure",
        "Fighting",
        "Shooter",
        "Hack and Slash",
        "Rhythm",
        "Platform",
        "Role-Playing",
        "RPG",
        "Racing",
        "Roguelike",
        "Shoot Em Up",
        "Simulation",
        " Sim",
        "Stealth-Based",
        "Strategy",
        "Survival Horror",
        "Tower Defense",
        "Sandbox",
        "4X",
        "Beat 'em Up",
        "Roguelike",
        "Roguelite",
    ];

    private readonly List<string> _folderLabelContainsBlacklist = ["Card ", "Board ", "Collectable ", "Collectible ", "Party ", "Tabletop ", "Game Show"];

    private readonly List<string> _folderLabelEqualsWhitelist = ["Action"];

    public override IEnumerable<TvTropesSearchResult> Search(string query)
    {
        var directUrlResult = GetBasicPageInfo(query);
        if (directUrlResult != null)
            return [directUrlResult];

        var results = Task.Run(async () => await GoogleSearch(query)).GetAwaiter().GetResult().ToList();
        results.RemoveAll(sr => sr.Breadcrumbs.Count != 1 || sr.Breadcrumbs[0] != "Tropes");

        return results;
    }

    public ParsedTropePage GetGamesForTrope(string url) => GetGamesForTrope(url, pageIsSubsection: false);

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

    private bool IsVideogameFolderName(string folderName)
    {
        if (_folderLabelContainsBlacklist.Any(l => folderName.Contains(l, StringComparison.InvariantCultureIgnoreCase)))
            return false;

        return _folderLabelContainsWhitelist.Any(l => folderName.Contains(l, StringComparison.InvariantCultureIgnoreCase))
               || _folderLabelEqualsWhitelist.Contains(folderName, StringComparer.InvariantCultureIgnoreCase);
    }

    private bool IsVideogameUrl(string url)
    {
        var linkSegments = GetWikiPathSegments(url);
        return linkSegments.Length == 2 && _videogameCategoryUrlRoots.Contains(linkSegments[0]);
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
        bool IsNonVideoGamesHeader(string header) => _nonLettersAndNumbers.Replace(header, "").Contains("nonvideogame", StringComparison.InvariantCultureIgnoreCase);

        void AddAllListElementsFromSegments(IList<Tuple<string, string>> hss)
        {
            if (hss.Count == 1)
                AddListElementsFromSourceString(hss[0].Item2);

            if (hss.Count > 1)
                foreach (var segment in hss)
                    if (!string.IsNullOrWhiteSpace(segment.Item1))
                        AddListElementsFromSourceString(segment.Item2);
        }

        if (!getAllUnfiltered)
        {
            bool nonVideoGameSegmentExists = headerSegments.Any(h => IsNonVideoGamesHeader(h.Item1));
            foreach (var segment in headerSegments)
            {
                var segmentHeader = segment.Item1;
                if (string.IsNullOrWhiteSpace(segmentHeader) || IsNonVideoGamesHeader(segmentHeader))
                    continue;

                var segmentContent = segment.Item2;
                if (IsVideogameFolderName(segmentHeader) || nonVideoGameSegmentExists)
                    AddListElementsFromSourceString(segmentContent);

                var folderLabels = htmlParser.Parse(segmentContent).QuerySelectorAll(".folderlabel[onclick^=\"togglefolder(\"]");

                var nonVideoGameFolderExists = folderLabels.Any(f => IsNonVideoGamesHeader(f.TextContent.HtmlDecode()));
                foreach (var folderLabel in folderLabels)
                {
                    var label = folderLabel.TextContent.HtmlDecode();
                    if (IsNonVideoGamesHeader(label))
                        continue;

                    if (IsVideogameFolderName(label) || nonVideoGameFolderExists)
                        output.AddRange(folderLabel.NextElementSibling.QuerySelectorAll("ul > li:has(em)"));
                }
            }
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
        var liChildren = element.Children.Where(c => c.TagName == "EM" || IsVideoGameLink(c)).ToList();

        var workNamesDeflated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        void AddWork(TvTropesWork work)
        {
            var deflatedName = _nonLettersAndNumbers.Replace(work.Title, string.Empty);
            if (workNamesDeflated.Add(deflatedName))
                output.Works.Add(work);
        }

        foreach (var lic in liChildren)
        {
            var work = new TvTropesWork { Title = lic.TextContent.HtmlDecode() };
            AddWork(work);
            var links = lic.TagName == "A"
                ? [lic]
                : lic.Children.Where(IsVideoGameLink).ToList();

            foreach (var a in links)
            {
                string absoluteUrl = GetAbsoluteUrl(a.GetAttribute("href"));
                work.Urls.Add(absoluteUrl);

                if (links.Count > 1)
                    AddWork(new() { Title = a.TextContent.HtmlDecode(), Urls = [absoluteUrl] });
            }
        }

        return output;
    }

    private readonly Regex _nonLettersAndNumbers = new(@"[^\p{L}0-9]", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private bool IsVideoGameLink(IElement element)
    {
        return element.TagName == "A" && UrlBelongsToWhitelistedWorkCategory(element.GetAttribute("href"));
    }

    public string ReverseEngineerGameNameFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var lastSegment = GetWikiPathSegments(url).Last();
        return ReverseEngineerGameNameFromUrlSegment(lastSegment);
    }

    private static string ReverseEngineerGameNameFromUrlSegment(string urlSegment)
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
