using HtmlAgilityPack;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using PlayniteExtensions.Common;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Barnite.Scrapers
{
    public class BolScraper : MetadataScraper
    {
        public override string Name { get; } = "bol.com";
        public override string WebsiteUrl { get; } = "https://www.bol.com";

        protected override string GetSearchUrlFromBarcode(string barcode)
        {
            return "https://www.bol.com/nl/nl/s/?searchtext=" + HttpUtility.UrlEncode(barcode);
        }

        protected override GameMetadata ScrapeGameDetailsHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//h1[@class='page-heading']")?.InnerText.HtmlDecode();
            var specsRows = doc.DocumentNode.SelectNodes("//div[@class='specs__row']");

            if (string.IsNullOrWhiteSpace(title) || specsRows.Count == 0)
                return null;

            var description = doc.DocumentNode.SelectSingleNode("//div[@class='product-description']")?.InnerHtml;
            var coverUrl = doc.DocumentNode.SelectSingleNode("//wsp-image-slot-slider//img[@src]")?.Attributes["src"].Value;

            title = TrimTitle(title);

            var data = new GameMetadata
            {
                Name = title,
                Description = description,
            };

            if (coverUrl != null)
                data.CoverImage = new MetadataFile(coverUrl);

            if (specsRows != null)
            {
                foreach (var specsNode in specsRows)
                {
                    var specName = specsNode.SelectSingleNode("./*[@class='specs__title']")?.InnerText.HtmlDecode();
                    var value = specsNode.SelectSingleNode("./*[@class='specs__value']")?.InnerText.HtmlDecode();
                    var values = value.Split(new[] { ", ", " | " }, StringSplitOptions.RemoveEmptyEntries);
                    switch (specName)
                    {
                        case "Merk":
                            data.Publishers = values.Select(v => new MetadataNameProperty(v)).ToHashSet<MetadataProperty>();
                            break;
                        case "Platform":
                            data.Platforms = values.SelectMany(PlatformUtility.GetPlatforms).ToHashSet();
                            break;
                        case "Genre":
                            data.Genres = values.Select(TranslateGenre).Select(g => new MetadataNameProperty(g)).ToHashSet<MetadataProperty>();
                            break;
                        case "PEGI-leeftijd":
                            data.AgeRatings = values.Select(v => new MetadataNameProperty("PEGI " + v.Replace("+", string.Empty))).ToHashSet<MetadataProperty>();
                            break;
                        case "Oorspronkelijke releasedatum":
                            if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime releaseDate))
                            {
                                data.ReleaseDate = new ReleaseDate(releaseDate);
                            }
                            break;
                        case "Regio":
                            data.Regions = new HashSet<MetadataProperty> { new MetadataNameProperty(value) };
                            break;
                        default:
                            break;
                    }
                }
            }

            return data;
        }

        private static string TranslateGenre(string genre)
        {
            switch (genre)
            {
                case "Actie": return "Action";
                case "Avontuur": return "Adventure";
                default: return genre;
            }
        }

        private string TrimTitle(string title)
        {
            var platformNames = PlatformUtility.GetPlatformNames();

            string output = title.TrimStart("Videogame - ");
            output = Regex.Replace(output, @"\[(?<platform>[-a-z 0-9]+)\]|\((?<platform>[-a-z 0-9]+)\)|[-/]\s*(?<platform>[a-z][a-z 0-9/]+)$", (match) =>
            {
                var platform = match.Groups["platform"]?.Value;
                if (platformNames.Any(p => string.Equals(p, platform, StringComparison.InvariantCultureIgnoreCase)))
                    return string.Empty;
                else
                    return match.Value;
            }, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture).Trim();

            return output;
        }

        protected override IEnumerable<GameLink> ScrapeSearchResultHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var links = doc.DocumentNode.SelectNodes("//a[@class='product-title px_list_page_product_click'][@href]");

            if (links == null)
                yield break;

            foreach (var link in links)
            {
                var url = GetAbsoluteUrl(link.Attributes["href"].Value);                
                var itemName = TrimTitle(link.InnerText.HtmlDecode());
                yield return new GameLink { Name = itemName, Url = url };
            }
        }
    }
}
