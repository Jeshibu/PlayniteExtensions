using HtmlAgilityPack;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System.Collections.Generic;
using System.Web;

namespace Barnite.Scrapers;

public class MobyGamesScraper : MetadataScraper
{
    public override string Name { get; } = "Moby Games";
    public override string WebsiteUrl { get; } = "https://www.mobygames.com";

    protected override string GetSearchUrlFromBarcode(string barcode)
    {
        return $"https://www.mobygames.com/search/?q={HttpUtility.UrlEncode(barcode)}&type=game";
    }

    protected override GameMetadata ScrapeGameDetailsHtml(string html)
    {
        return new MobyGamesHelper(PlatformUtility).ParseGameDetailsHtml(html)?.ToMetadata();
    }

    protected override IEnumerable<GameLink> ScrapeSearchResultHtml(string html)
    {
        var page = new HtmlDocument();
        page.LoadHtml(html);

        var cells = page.DocumentNode.SelectNodes("//table[@class='table mb']/tr/td[last()]");
        if (cells == null)
            yield break;

        foreach (var td in cells)
        {
            if (!td.InnerText.Contains("Product code: "))
                continue;

            var a = td.SelectSingleNode(".//a[@href]");
            if (a == null)
                continue;

            yield return new GameLink { Url = a.Attributes["href"].Value, Name = a.InnerText.HtmlDecode() };
        }
    }
}
