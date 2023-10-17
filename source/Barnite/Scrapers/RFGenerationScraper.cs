using HtmlAgilityPack;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Barnite.Scrapers
{
    public class RFGenerationScraper : DuckDuckGoScraper
    {
        public override string Name { get; } = "RF Generation";

        public override string WebsiteUrl { get; } = "https://www.rfgeneration.com";

        protected override string SearchDomain { get; } = "rfgeneration.com";

        protected override bool IsGameUrl(string url)
        {
            return Regex.IsMatch(url, @"^https://(www\.)?rfgeneration\.com/cgi-bin/getinfo\.pl\?ID=[A-Z0-9-]+$");
        }

        protected override GameMetadata ScrapeGameDetailsHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//tr[@id='title']/td[1]/div[@class='headline']")?.InnerText.HtmlDecode();

            if (string.IsNullOrWhiteSpace(title))
                return null;

            var data = new GameMetadata { Name = title };

            var rows = doc.DocumentNode.SelectNodes("//tr[count(td)=2 and td[@class='headline']]");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var key = row.SelectSingleNode("./td[@class='headline']")?.InnerText.HtmlDecode();
                    var value = row.SelectSingleNode("./td[2]").InnerText.HtmlDecode();
                    switch (key)
                    {
                        case "Console:":
                            data.Platforms = PlatformUtility.GetPlatforms(value).ToHashSet();
                            break;
                        case "Region:":
                            string[] regions = value.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            data.Regions = regions.Select(r => new MetadataNameProperty(r.Trim())).ToHashSet<MetadataProperty>();
                            break;
                        case "Developer:":
                            data.Developers = new HashSet<MetadataProperty> { new MetadataNameProperty(value) };
                            break;
                        case "Publisher:":
                            data.Publishers = new HashSet<MetadataProperty> { new MetadataNameProperty(value) };
                            break;
                        case "Year:":
                            if (int.TryParse(value, out int year))
                                data.ReleaseDate = new ReleaseDate(year);
                            break;
                        default:
                            break;
                    }
                }
            }

            return data;
        }
    }
}
