using HtmlAgilityPack;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Barnite.Scrapers
{
    public class OgdbScraper : MetadataScraper
    {
        public override string Name { get; } = "OGDB";
        public override string WebsiteUrl { get; } = "https://ogdb.eu/";
        private Regex EndBracesTextRegex = new Regex(@"(\s+(\([^)]+\)|\[[^]]+\]))+\s*$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        protected override string GetSearchUrlFromBarcode(string barcode)
        {
            return $"https://ogdb.eu/index.php?section=simplesearchresults&searchstring={HttpUtility.UrlEncode(barcode)}&how=AND";
        }

        protected override GameMetadata ScrapeGameDetailsHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            string name = doc.DocumentNode.SelectSingleNode("//td[@class='thead']")?.InnerText;
            if (name == null)
                return null;

            name = EndBracesTextRegex.Replace(name.HtmlDecode(), string.Empty);

            if (name == "Ergebnisse einfacher Suche") //search page title
                return null;

            var game = new GameMetadata
            {
                Name = name,
            };

            var coverImg = doc.DocumentNode.SelectSingleNode("//img[starts-with(@src, '/imageview.php?image_id=')]");
            if (coverImg != null)
            {
                string coverUrl = GetAbsoluteUrl(coverImg.Attributes["src"].Value);
                int limitIndex = coverUrl.IndexOf("&limit=");
                if (limitIndex != -1)
                {
                    coverUrl = coverUrl.Remove(limitIndex) + "&limit=400";
                }

                game.CoverImage = new MetadataFile(coverUrl);
            }

            var tableRows = doc.DocumentNode.SelectNodes("//tr[./td[@class='tboldc'] and ./td[starts-with(@class, 'tnorm')]]");
            if (tableRows != null)
            {
                foreach (HtmlNode tableRow in tableRows)
                {
                    var prop = tableRow.SelectSingleNode("td[@class='tboldc']")?.InnerText.HtmlDecode().TrimEnd(':').ToLowerInvariant();
                    var valueNode = tableRow.SelectSingleNode("td[starts-with(@class, 'tnorm')]");
                    var value = valueNode?.InnerText.HtmlDecode();

                    if (prop == null || value == null)
                        continue;

                    switch (prop)
                    {
                        case "system":
                            string[] values = value.Split('/')
                                .Select(p => p.Replace(" - Download", ""))
                                .Where(p => !p.StartsWith("PC - ")) //these are handled with the Betriebsystem (OS)
                                .ToArray();
                            game.Platforms = values.SelectMany(PlatformUtility.GetPlatforms).ToHashSet();
                            break;
                        case "betriebsystem":
                            if (value.StartsWith("Windows"))
                                game.Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") };
                            else if (value == "MS-DOS")
                                game.Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_dos") };
                            else if (value.Contains("Linux"))
                                game.Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_linux") };
                            break;
                        case "erschienen":
                                game.ReleaseDate = ParseReleaseDate(value);
                            break;
                        case "entwickler":
                            var devs = valueNode.SelectNodes(".//a")?.Select(a => a.InnerText.HtmlDecode());
                            if (devs != null)
                                game.Developers = devs.Select(d => new MetadataNameProperty(d)).ToHashSet<MetadataProperty>();
                            break;
                        case "publisher":
                            var publishers = valueNode.SelectNodes(".//a")?.Select(a => a.InnerText.HtmlDecode());
                            if (publishers != null)
                                game.Publishers = publishers.Select(d => new MetadataNameProperty(d.TrimCompanyForms())).ToHashSet<MetadataProperty>();
                            break;
                    }
                }
            }

            return game;
        }

        protected override IEnumerable<GameLink> ScrapeSearchResultHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var linkNodes = doc.DocumentNode.SelectNodes("//span[@id='ta2']/a[@href]");
            if (linkNodes != null)
            {
                foreach (var link in linkNodes)
                {
                    yield return new GameLink
                    {
                        Name = link.InnerText.HtmlDecode(),
                        Url = GetAbsoluteUrl(link.Attributes["href"].Value)
                    };
                }
            }
        }

        private ReleaseDate ParseReleaseDate(string releaseDateStr)
        {
            if (string.IsNullOrWhiteSpace(releaseDateStr))
                return new ReleaseDate();

            var segments = releaseDateStr.Split('.');
            if (!segments.SelectMany(s => s.ToCharArray()).All(char.IsNumber))
                return new ReleaseDate();

            var segmentNumbers = segments.Select(int.Parse).ToList();
            switch (segmentNumbers.Count)
            {
                case 1: return new ReleaseDate(segmentNumbers[0]);
                case 2: return new ReleaseDate(segmentNumbers[1], segmentNumbers[0]);
                case 3: return new ReleaseDate(segmentNumbers[2], segmentNumbers[1], segmentNumbers[0]);
                default: return new ReleaseDate();
            }
        }
    }
}
