using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Barnite.Scrapers
{
    public class MobyGamesScraper : MetadataScraper
    {
        public override string Name { get; } = "Moby Games";
        public override string WebsiteUrl { get; } = "https://www.mobygames.com";

        private static List<Tuple<string, string[]>> GetDivPropertyValues(HtmlAgilityPack.HtmlDocument doc, string xpath)
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            var output = new List<Tuple<string, string[]>>();

            if (nodes == null)
                return output;

            for (int i = 0; i + 1 < nodes.Count; i += 2)
            {
                string property = nodes[i].InnerText;
                var valueNode = nodes[i + 1];
                var values = valueNode.SelectNodes("a").Select(n => n.InnerText.HtmlDecode()).ToArray();
                output.Add(new Tuple<string, string[]>(property, values));
            }
            return output;
        }

        protected override string GetSearchUrlFromBarcode(string barcode)
        {
            return "https://www.mobygames.com/search/quick?q=" + HttpUtility.UrlEncode(barcode);
        }

        protected override GameMetadata ScrapeGameDetailsHtml(string html)
        {
            var page = new HtmlAgilityPack.HtmlDocument();
            page.LoadHtml(html);
            var title = page.DocumentNode.SelectSingleNode(@"//h1[@class='niceHeaderTitle']/a[1]")?.InnerText.HtmlDecode();
            if (title == null) return null;

            var platformName = page.DocumentNode.SelectSingleNode(@"//h1[@class='niceHeaderTitle']/small/a")?.InnerText.HtmlDecode();
            var data = new GameMetadata
            {
                Name = title,
                Platforms = new HashSet<MetadataProperty>(PlatformUtility.GetPlatforms(platformName)),
                Genres = new HashSet<MetadataProperty>(),
                AgeRatings = new HashSet<MetadataProperty>(),
                Tags = new HashSet<MetadataProperty>(),
            };

            var coverImgsrc = page.DocumentNode.SelectSingleNode("//div[@id='coreGameCover']/a/img[@src]")?.Attributes["src"].Value;
            if (coverImgsrc != null)
            {
                coverImgsrc = coverImgsrc.Replace("/s/", "/l/"); //change the cover image url from the thumbnail to the full size image

                if (!coverImgsrc.StartsWith("//") && !coverImgsrc.StartsWith("https://") && !coverImgsrc.StartsWith("http://") && coverImgsrc.StartsWith("/"))
                    coverImgsrc = "https://www.mobygames.com" + coverImgsrc;

                data.CoverImage = new MetadataFile(coverImgsrc);
            }

            var releaseValues = GetDivPropertyValues(page, "//div[@id='coreGameRelease']/div");
            foreach (var x in releaseValues)
            {
                switch (x.Item1)
                {
                    case "Published by":
                        data.Publishers = x.Item2.Select(n => new MetadataNameProperty(n)).ToHashSet<MetadataProperty>();
                        break;
                    case "Developed by":
                        data.Developers = x.Item2.Select(n => new MetadataNameProperty(n)).ToHashSet<MetadataProperty>();
                        break;
                    case "Released":
                        if (DateTime.TryParseExact(x.Item2.First(), "MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var releaseDate))
                            data.ReleaseDate = new ReleaseDate(releaseDate);
                        break;
                }
            }

            var genreValues = GetDivPropertyValues(page, "//div[@id='coreGameGenre']/div[last()]/div");
            foreach (var x in genreValues)
            {
                if (x.Item1.EndsWith(" Rating"))
                {
                    foreach (var rating in x.Item2)
                    {
                        //property will be 'ESRB Rating'/'PEGI Rating' and value 'Mature'/'18', so combine them into 'ESRB Mature'/'PEGI 18'
                        data.AgeRatings.Add(new MetadataNameProperty(x.Item1.Remove(x.Item1.Length - "Rating".Length) + rating));
                    }
                    continue;
                }
                switch (x.Item1)
                {
                    case "Genre":
                    case "Perspective":
                    case "Gameplay":
                    case "Narrative":
                        foreach (var item in x.Item2)
                        {
                            data.Genres.Add(new MetadataNameProperty(item));
                        }
                        break;
                    case "Interface":
                    case "Setting":
                    case "Misc":
                    default:
                        foreach (var item in x.Item2)
                        {
                            data.Tags.Add(new MetadataNameProperty(item));
                        }
                        break;
                }
            }

            var descriptionHeaderNode = page.DocumentNode.SelectSingleNode("//h2[text()='Description']");
            if (descriptionHeaderNode != null)
            {
                var description = "";
                var node = descriptionHeaderNode;
                while ((node = node.NextSibling) != null)
                {
                    if (node.Name == "div" && node.Attributes.Any(a => a.Name == "class" && a.Value == "sideBarLinks"))
                        break;

                    description += node.OuterHtml;
                }
                data.Description = description.Trim();
            }

            return data;
        }

        protected override IEnumerable<GameLink> ScrapeSearchResultHtml(string html)
        {
            var page = new HtmlAgilityPack.HtmlDocument();
            page.LoadHtml(html);

            var links = page.DocumentNode.SelectNodes("//div[@class='searchTitle']/a");
            if (links == null)
                yield break;

            foreach (var a in links)
            {
                yield return new GameLink { Url = a.Attributes["href"].Value, Name = a.InnerText.HtmlDecode() };
            }
        }
    }
}
