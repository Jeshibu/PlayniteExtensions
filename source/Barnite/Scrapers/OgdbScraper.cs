using HtmlAgilityPack;
using Playnite.SDK.Models;
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
        public OgdbScraper(IPlatformUtility platformUtility, IWebclient webclient) : base(platformUtility, webclient)
        {
        }

        public override string Name => "OGDB";
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

            name = EndBracesTextRegex.Replace(HtmlDecodeAndNormalizeWhitespace(name), string.Empty);

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
                    var prop = HtmlDecodeAndNormalizeWhitespace(tableRow.SelectSingleNode("td[@class='tboldc']")?.InnerText)?.TrimEnd(':').ToLowerInvariant();
                    var valueNode = tableRow.SelectSingleNode("td[starts-with(@class, 'tnorm')]");
                    var value = HtmlDecodeAndNormalizeWhitespace(valueNode?.InnerText);

                    if (prop == null || value == null)
                        continue;

                    switch (prop)
                    {
                        case "system":
                            string[] values = value.Split('/')
                                .Select(p => p.Replace(" - Download", ""))
                                .Where(p => !p.StartsWith("PC - ")) //these are handled with the Betriebsystem (OS)
                                .ToArray();
                            game.Platforms = values.Select(PlatformUtility.GetPlatform).ToHashSet();
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
                            if (DateTime.TryParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime releaseDate))
                                game.ReleaseDate = new ReleaseDate(releaseDate);
                            break;
                        case "entwickler":
                            var devs = valueNode.SelectNodes("a")?.Select(a => HtmlDecodeAndNormalizeWhitespace(a.InnerText));
                            if (devs != null)
                                game.Developers = devs.Select(d => new MetadataNameProperty(d)).ToHashSet<MetadataProperty>();
                            break;
                        case "publisher":
                            var publishers = valueNode.SelectNodes("a")?.Select(a => HtmlDecodeAndNormalizeWhitespace(a.InnerText));
                            if (publishers != null)
                                game.Publishers = publishers.Select(d => new MetadataNameProperty(d)).ToHashSet<MetadataProperty>();
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
            foreach (var link in linkNodes)
            {
                yield return new GameLink
                {
                    Name = HtmlDecodeAndNormalizeWhitespace(link.InnerText),
                    Url = GetAbsoluteUrl(link.Attributes["href"].Value)
                };
            }
        }
    }
}
