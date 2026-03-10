using AngleSharp.Parser.Html;
using IgnMetadata.HowLongToBeat.Models;
using PlayniteExtensions.Common;
using System;
using System.Linq;

namespace IgnMetadata.HowLongToBeat;

public class IgnScraper(IWebDownloader downloader)
{
    public IgnHltbDataModel GetHltbData(string ignGameUrl)
    {
        var response = downloader.DownloadString(ignGameUrl);
        var doc = new HtmlParser().Parse(response.ResponseContent);

        var hltbElement = doc.QuerySelector(".hl2b-box");
        var platformElements = doc.QuerySelectorAll("[data-cy=platforms-info] [data-cy=platforms-container] > a[href][title]");

        var output = new IgnHltbDataModel();
        output.IgnUrl = ignGameUrl;
        output.Name = doc.QuerySelector("[data-cy=object-header-display-title]").TextContent;
        output.CoverUrl = doc.QuerySelector("[data-cy=object-thumbnail] img[src]")?.GetAttribute("src");
        output.HltbUrl = hltbElement?.QuerySelector("a[href][data-cy=hl2b-link]")?.GetAttribute("href");
        output.MainStoryHours = GetHours("main-story-meta-item");
        output.MainStoryAndSidesHours = GetHours("story-sides-meta-item");
        output.EverythingHours = GetHours("everything-meta-item");
        output.AllStylesHours = GetHours("all-styles-meta-item");
        output.Platforms = platformElements?.Select(e => e.GetAttribute("title")).ToList();
        return output;

        int GetHours(string dataCyValue)
        {
            string hoursString = hltbElement?.QuerySelector($"[data-cy={dataCyValue}] > h4")?.TextContent;
            return int.TryParse(hoursString?.TrimEnd([" hrs", " hr"]), out int hours) ? hours : 0;
        }
    }
}
