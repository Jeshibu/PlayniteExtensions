using HtmlAgilityPack;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Barnite.Scrapers
{
    public class MobyGamesScraper : MetadataScraper
    {
        public override string Name { get; } = "Moby Games";
        public override string WebsiteUrl { get; } = "https://www.mobygames.com";

        private static List<string> GetMetadataListItems(HtmlDocument doc, string propName)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//dl[@class='metadata']/dt[text()='{propName}']/following-sibling::dd[1]/a");
            var output = nodes?.Select(n => n.InnerText).ToList();
            return output ?? new List<string>();
        }

        private static void SetMetadataListItems(HashSet<MetadataProperty> metadataCollection, HtmlDocument doc, string propName)
        {
            foreach (var item in GetMetadataListItems(doc, propName))
            {
                metadataCollection.Add(new MetadataNameProperty(item.HtmlDecode()));
            }
        }

        protected override string GetSearchUrlFromBarcode(string barcode)
        {
            return $"https://www.mobygames.com/search/?q={HttpUtility.UrlEncode(barcode)}&type=game";
        }

        protected override GameMetadata ScrapeGameDetailsHtml(string html)
        {
            var page = new HtmlDocument();
            page.LoadHtml(html);
            var title = page.DocumentNode.SelectSingleNode(@"//h1")?.InnerText.HtmlDecode();
            if (title == null) return null;

            var platforms = page.DocumentNode.SelectNodes("//ul[@id='platformLinks']/li//a[starts-with(@href, '/game/platform:')]")?.SelectMany(a => PlatformUtility.GetPlatforms(a.InnerText.HtmlDecode())).ToHashSet();

            var data = new GameMetadata
            {
                Name = title,
                Platforms = platforms ?? new HashSet<MetadataProperty>(),
                Genres = new HashSet<MetadataProperty>(),
                Tags = new HashSet<MetadataProperty>(),
            };

            var coverImgsrc = page.DocumentNode.SelectSingleNode("//a[@id='cover']/img[@src]")?.Attributes["src"].Value;
            if (coverImgsrc != null)
            {
                var coverUri = new Uri(new Uri("https://www.mobygames.com/"), coverImgsrc);

                data.CoverImage = new MetadataFile(coverUri.AbsoluteUri);
            }

            data.Publishers = page.DocumentNode.SelectNodes("//ul[@id='publisherLinks']/li/a")?.Select(p => new MetadataNameProperty(p.InnerText.HtmlDecode().TrimCompanyForms())).ToHashSet<MetadataProperty>();
            data.Developers = page.DocumentNode.SelectNodes("//ul[@id='developerLinks']/li/a")?.Select(p => new MetadataNameProperty(p.InnerText.HtmlDecode().TrimCompanyForms())).ToHashSet<MetadataProperty>();

            var releaseDateString = page.DocumentNode.SelectSingleNode("//dl[@class='metadata']//a[1]")?.InnerText.HtmlDecode();
            if (releaseDateString != null)
            {
                releaseDateString = Regex.Replace(releaseDateString, @"(?<=[0-9])(st|nd|rd|th)", "");
                if (DateTime.TryParseExact(releaseDateString, "MMMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var releaseDate))
                    data.ReleaseDate = new ReleaseDate(releaseDate);
            }

            SetMetadataListItems(data.Genres, page, "Genre");
            SetMetadataListItems(data.Genres, page, "Perspective");
            SetMetadataListItems(data.Genres, page, "Gameplay");
            SetMetadataListItems(data.Genres, page, "Narrative");
            SetMetadataListItems(data.Tags, page, "Interface");
            SetMetadataListItems(data.Tags, page, "Setting");
            SetMetadataListItems(data.Tags, page, "Misc");

            data.Description = page.DocumentNode.SelectSingleNode("//div[@id='description-text']")?.InnerHtml.Trim();

            return data;
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
}
