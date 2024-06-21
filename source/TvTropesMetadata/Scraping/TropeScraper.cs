using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TvTropesMetadata.Scraping
{
    public class TropeScraper : BaseScraper
    {
        public List<string> SubcategoryWhitelist = new List<string> { "VideoGames", "VisualNovels" };
        public List<string> FolderLabelWhitelist = new List<string> { "Video Game", "Videogame", "Visual Novel" };

        public TropeScraper(IWebDownloader downloader) : base(downloader) { }

        public override IEnumerable<TvTropesSearchResult> Search(string query) => Search(query, "trope");

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
            if (!pageIsSubsection)
            {
                var subcategoryUrls = GetSubcategoryUrls(doc, url);
                foreach (var subcategoryUrl in subcategoryUrls)
                {
                    var subcategoryPage = GetGamesForTrope(subcategoryUrl, pageIsSubsection: true);
                    output.Items.AddRange(subcategoryPage.Items);
                }
                output.Items.RemoveAll(i => i.Works.Count == 0 || BlacklistedWords.Any(w => i.Text.Contains(w)));
            }
            return output;
        }

        private IEnumerable<string> GetSubcategoryUrls(IHtmlDocument doc, string pageUrl)
        {
            var lastUrlSegment = GetWikiPathSegments(pageUrl).Last();
            var links = doc.QuerySelectorAll(".article-content > ul > li > a.twikilink[href]");
            foreach (var a in links)
            {
                var linkUrl = a.GetAttribute("href");
                var linkSegments = GetWikiPathSegments(linkUrl);
                if (linkSegments.Length != 2)
                    continue;

                foreach (var sub in SubcategoryWhitelist)
                {
                    if (linkSegments[0].Equals(lastUrlSegment, StringComparison.InvariantCultureIgnoreCase) && linkSegments[1].Contains(sub, StringComparison.InvariantCultureIgnoreCase))
                        yield return linkUrl.GetAbsoluteUrl(pageUrl);
                }
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
                    if (FolderLabelWhitelist.Any(l => segmentHeader.Contains(l, StringComparison.InvariantCultureIgnoreCase)))
                        AddListElementsFromSourceString(segmentContent);

                    var segmentDoc = htmlParser.Parse(segmentContent);
                    var folderLabels = segmentDoc.QuerySelectorAll(".folderlabel[onclick^=\"togglefolder(\"]");
                    foreach (var folderLabel in folderLabels)
                    {
                        var label = folderLabel.TextContent.HtmlDecode();
                        if (FolderLabelWhitelist.Any(l => label.Contains(l, StringComparison.InvariantCultureIgnoreCase)))
                            output.AddRange(folderLabel.NextElementSibling.QuerySelectorAll("ul > li:has(em)"));
                    }
                }
            }

            void AddAllListElementsFromSegments(IList<Tuple<string,string>> hss)
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
            var output = new TropePageListItem { Text = element.InnerHtml.Split(new[] { "<ul>" }, StringSplitOptions.RemoveEmptyEntries).First() };
            var liChildren = element.Children.Where(c => c.TagName == "EM" || IsVideoGameLink(c));
            if (liChildren == null)
                return output;

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
                    ? new[] { lic }
                    : lic.Children.Where(IsVideoGameLink);

                foreach (var a in links)
                {
                    var url = GetAbsoluteUrl(a.GetAttribute("href"));
                    work.Urls.Add(url);
                    var gameUrlName = GetWikiPathSegments(url).Last();
                    AddWork(new TvTropesWork { Title = gameUrlName, Urls = new List<string> { url } });
                }
            }
            return output;
        }

        private Regex NonLettersAndNumbers = new Regex(@"[^\p{L}0-9]", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private bool IsVideoGameLink(IElement element)
        {
            return element.TagName == "A" && UrlBelongsToWhitelistedWorkCategory(element.GetAttribute("href"));
        }
    }

    public class ParsedTropePage
    {
        public string Title { get; set; }
        public List<TropePageListItem> Items { get; set; } = new List<TropePageListItem>();
    }

    public class TropePageListItem
    {
        public string Text { get; set; }

        public List<TvTropesWork> Works { get; set; } = new List<TvTropesWork>();
    }

    public class TvTropesWork
    {
        public string Title { get; set; }
        public List<string> Urls { set; get; } = new List<string>();
    }
}
