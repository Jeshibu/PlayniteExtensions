using HtmlAgilityPack;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Barnite.Scrapers;

public class RetroplaceScraper : MetadataScraper
{
    public override string Name { get; } = "Retroplace";

    public override string WebsiteUrl { get; } = "https://www.retroplace.com";

    protected override string GetSearchUrlFromBarcode(string barcode)
    {
        return "https://www.retroplace.com/en/games/marketplace?barcode=" + HttpUtility.UrlEncode(barcode);
    }

    protected override GameMetadata ScrapeGameDetailsHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        string name = doc.DocumentNode.SelectSingleNode("//h1/span[@itemprop='name']")?.InnerText.HtmlDecode();
        string platform = doc.DocumentNode.SelectSingleNode("//span[@itemprop='gamePlatform']")?.InnerText.HtmlDecode();

        if (name == null)
            return null;

        var data = new GameMetadata
        {
            Name = name,
            Platforms = PlatformUtility.GetPlatforms(platform).ToHashSet()
        };

        data.Genres = doc.DocumentNode.SelectNodes("//span[@itemprop='genre']")
            ?.Select(span => new MetadataNameProperty(span.InnerText.HtmlDecode()))
            .ToHashSet<MetadataProperty>();

        var author = doc.DocumentNode.SelectSingleNode("//span[@itemprop='author']")?.InnerText.HtmlDecode();
        if (author != null)
            data.Developers = [new MetadataNameProperty(author.TrimCompanyForms())];

        var publisher = doc.DocumentNode.SelectSingleNode("//span[@itemprop='publisher']")?.InnerText.HtmlDecode();
        if (publisher != null)
            data.Publishers = [new MetadataNameProperty(publisher.TrimCompanyForms())];

        var releaseDateString = doc.DocumentNode.SelectSingleNode("//time[@datetime]")?.Attributes["datetime"].Value;
        if (DateTime.TryParseExact(releaseDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime releaseDate))
            data.ReleaseDate = new ReleaseDate(releaseDate);

        return data;
    }

    protected override IEnumerable<GameLink> ScrapeSearchResultHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var linkNodes = doc.DocumentNode.SelectNodes("//a[@class='GEC-item'][@title][@href]");

        if (linkNodes == null)
            yield break;

        foreach (var linkNode in linkNodes)
        {
            var name = linkNode.InnerText.HtmlDecode();
            var url = GetAbsoluteUrl(linkNode.Attributes["href"].Value);
            yield return new GameLink { Name = name, Url = url };
        }
    }
}
