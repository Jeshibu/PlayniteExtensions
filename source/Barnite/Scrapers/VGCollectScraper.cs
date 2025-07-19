using HtmlAgilityPack;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Barnite.Scrapers;

public class VGCollectScraper : DuckDuckGoScraper
{
    public override string Name { get; } = "VGCollect";

    public override string WebsiteUrl { get; } = "https://vgcollect.com";

    protected override string SearchDomain { get; } = "vgcollect.com";

    protected override bool IsGameUrl(string url)
    {
        return Regex.IsMatch(url, @"^https://vgcollect\.com/item/[0-9]+$");
    }

    protected override GameMetadata ScrapeGameDetailsHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = doc.DocumentNode.SelectSingleNode("//div[@class='item-header']/h2")?.InnerText.HtmlDecode();

        if (string.IsNullOrWhiteSpace(title))
            return null;

        title = Regex.Replace(title, @" \([A-Z0-9]+\)$", string.Empty);

        var data = new GameMetadata { Name = title };

        var platformName = doc.DocumentNode.SelectSingleNode("//td[@class='item-platform']")?.InnerText.HtmlDecode();
        if (!string.IsNullOrEmpty(platformName))
        {
            var platformMatch = Regex.Match(platformName, @"^(?<platform>[a-z][a-z0-9 -]+[a-z0-9]) +\[(?<region>[a-z]+)\]$", RegexOptions.IgnoreCase);
            if (platformMatch.Success)
            {
                var platforms = PlatformUtility.GetPlatforms(platformMatch.Groups["platform"].Value);
                var region = platformMatch.Groups["region"].Value;
                data.Platforms = platforms.ToHashSet();
                data.Regions = [new MetadataNameProperty(region)];
            }
            else
            {
                data.Platforms = new HashSet<MetadataProperty>(PlatformUtility.GetPlatforms(platformName));
            }
        }
        var coverUrl = doc.DocumentNode.SelectSingleNode("//div[@id='item-image-front']/img[@src]")?.Attributes["src"].Value;
        if (coverUrl != null)
            data.CoverImage = new MetadataFile(coverUrl);

        return data;
    }
}
