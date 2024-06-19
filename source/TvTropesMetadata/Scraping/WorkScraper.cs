using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TvTropesMetadata.Scraping
{
    public class WorkScraper : BaseScraper
    {
        public WorkScraper(IWebDownloader downloader) : base(downloader) { }

        public override IEnumerable<TvTropesSearchResult> Search(string query) => Search(query, "work");

        public ParsedWorkPage GetTropesForGame(string url, bool pageIsSubsection = false)
        {
            var doc = GetDocument(url);
            var output = new ParsedWorkPage { Title = GetTitle(doc) };
            output.Tropes.AddRange(GetTropesOnPage(doc));
            if (!pageIsSubsection)
            {
                output.CoverImageUrl = GetCoverImageUrl(doc);
                output.Description = GetDescription(doc);
                output.Franchises.AddRange(GetFranchises(doc));

                var subcategoryUrls = GetSubcategoryUrls(doc, url);
                foreach (var subcategoryUrl in subcategoryUrls)
                {
                    var subcategoryPage = GetTropesForGame(subcategoryUrl, pageIsSubsection: true);
                    output.Tropes.AddRange(subcategoryPage.Tropes);
                }
            }
            return output;
        }

        private IEnumerable<string> GetTropesOnPage(IHtmlDocument doc)
        {
            var listItems = doc.QuerySelectorAll(".article-content > ul > li:has(a[href*=\"/Main/\"]), .article-content > .folder > ul > li:has(a[href*=\"/Main/\"])");
            foreach (var item in listItems)
            {
                var text = item.TextContent;
                if (BlacklistedWords.Any(w => text.Contains(w, System.StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                var a = item.QuerySelector("a[href*=\"/Main/\"]");
                yield return a.TextContent.HtmlDecode();
            }
        }

        private IEnumerable<string> GetSubcategoryUrls(IHtmlDocument doc, string pageUrl)
        {
            var lastUrlSegment = GetWikiPathSegments(pageUrl).Last();
            var links = doc.QuerySelectorAll(".article-content > ul > li > a.twikilink[href]");
            foreach (var a in links)
            {
                var linkUrl = a.GetAttribute("href");
                var linkSegments = GetWikiPathSegments(linkUrl);
                if (linkSegments.Length == 2 && linkSegments[0] == lastUrlSegment && linkSegments[1].StartsWith("Tropes"))
                    yield return linkUrl.GetAbsoluteUrl(pageUrl);
            }
        }

        private string GetCoverImageUrl(IHtmlDocument doc)
        {
            var img = doc.QuerySelector("img.embeddedimage[src]");
            return img?.GetAttribute("src");
        }

        private string GetDescription(IHtmlDocument doc)
        {
            var articleContent = doc.QuerySelector(".article-content")?.InnerHtml.Trim();
            if (articleContent == null) return null;
            var descriptionString = articleContent.Split(new[] { "<h2>" }, StringSplitOptions.RemoveEmptyEntries).First();

            var descriptionDoc = new HtmlParser().Parse(descriptionString);
            var removeElements = descriptionDoc.QuerySelectorAll(".quoteright, .acaptionright");
            foreach (var r in removeElements)
                r.Remove();

            var links = descriptionDoc.QuerySelectorAll("a[href]");
            foreach (var a in links)
            {
                var absoluteUrl = a.GetAttribute("href").GetAbsoluteUrl("https://tvtropes.org/");
                a.SetAttribute("href", absoluteUrl);
            }
            return descriptionDoc.Body.InnerHtml;
        }

        private IEnumerable<string> GetFranchises(IHtmlDocument doc)
        {
            var links = doc.QuerySelectorAll(".section-links > .links > ul > li:nth-child(2) > a[href*=\"/Franchise/\"]");
            if (links == null)
                yield break;

            foreach (var a in links)
                yield return a.TextContent.HtmlDecode().TrimStart("Franchise/");
        }
    }

    public class ParsedWorkPage
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverImageUrl { get; set; }
        public List<string> Tropes { get; set; } = new List<string>();
        public List<string> Franchises { get; set; } = new List<string>();
    }
}
