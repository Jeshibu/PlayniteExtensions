using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TvTropesMetadata.Scraping
{
    public class TropeScraper : BaseScraper
    {
        public List<string> SubcategoryWhitelist = new List<string> { "VideoGames", "VisualNovels" };
        public List<string> FolderLabelWhitelist = new List<string> { "Video Game", "Videogame", "Visual Novel" };

        public TropeScraper(IWebDownloader downloader) : base(downloader) { }

        public override IEnumerable<TvTropesSearchResult> Search(string query) => Search(query, "trope");

        public ParsedTropePage GetGamesForTrope(string url, bool pageIsSubsection = false)
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

        private IEnumerable<IElement> GetTropeListItems(IHtmlDocument doc)
        {
            var content = doc.QuerySelector(".article-content")?.InnerHtml;
            if (content == null) yield break;

        }

        private List<IElement> GetTropePageListElements(string content, bool getAllUnfiltered = false)
        {
            var htmlParser = new HtmlParser();
            var headerSegments = GetHeaderSegments(content).ToList();
            var output = new List<IElement>();

            void AddListElementsFromSourceString(string source) => output.AddRange(htmlParser.Parse(source).QuerySelectorAll("ul > li:has(em)"));

            if (!getAllUnfiltered)
            {
                foreach (var segment in headerSegments)
                {
                    var segmentHeader = segment.Item1;
                    if (string.IsNullOrWhiteSpace(segmentHeader))
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

            if (output.Count == 0)
            {
                if (headerSegments.Count == 1)
                    AddListElementsFromSourceString(headerSegments[0].Item2);

                if (headerSegments.Count > 1)
                    foreach (var segment in headerSegments)
                        if (!string.IsNullOrWhiteSpace(segment.Item1))
                            AddListElementsFromSourceString(segment.Item2);
            }
            return output;
        }

        private TropePageListItem ParseTropePageListItem(IElement element)
        {
            var output = new TropePageListItem { Text = element.TextContent };
            var emElements = element.QuerySelectorAll("em");
            if (emElements == null)
                return output;

            foreach (var em in emElements)
            {
                var work = new TvTropesWork { Title = em.TextContent.HtmlDecode() };
                output.Works.Add(work);
                var aElements = em.QuerySelectorAll("a[href]");
                foreach (var a in aElements)
                    work.Urls.Add(a.GetAttribute("href").GetAbsoluteUrl("https://tvtropes.org/"));
            }
            return output;
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
