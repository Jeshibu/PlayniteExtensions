using HtmlAgilityPack;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Barnite.Scrapers;

public class MobyGamesHelper : MobyGamesIdUtility
{
    public MobyGamesHelper(IPlatformUtility platformUtility)
    {
        PlatformUtility = platformUtility;
    }

    public IPlatformUtility PlatformUtility { get; }

    public string GetMobyGameIdStringFromUrl(string url)
    {
        return GetIdFromUrl(url).Id;
    }

    public int? GetMobyGameIdFromUrl(string url)
    {
        var id = GetIdFromUrl(url);
        if (id.Database == ExternalDatabase.None)
            return null;

        return int.Parse(id.Id);
    }

    public GameDetails ParseGameDetailsHtml(string html, bool parseGenres = true)
    {
        var page = new HtmlDocument();
        page.LoadHtml(html);
        var title = page.DocumentNode.SelectSingleNode(@"//h1")?.InnerText.HtmlDecode();
        if (title == null) return null;

        var platforms = page.DocumentNode.SelectNodes("//ul[@id='platformLinks']/li//a[starts-with(@href, '/game/platform:')]")?.SelectMany(a => PlatformUtility.GetPlatforms(a.InnerText.HtmlDecode())).ToHashSet();

        var data = new GameDetails();
        data.Names.Add(title);
        if (platforms != null)
            data.Platforms.AddRange(platforms);

        var coverImgsrc = page.DocumentNode.SelectSingleNode("//a[@id='cover']/img[@src]")?.Attributes["src"].Value;
        if (coverImgsrc != null)
        {
            var coverUri = new Uri(new Uri("https://www.mobygames.com/"), coverImgsrc);

            data.CoverOptions.Add(new BasicImage(coverUri.AbsoluteUri));
        }

        data.Publishers = page.DocumentNode.SelectNodes("//ul[@id='publisherLinks']/li/a")?.Select(p => p.InnerText.HtmlDecode().TrimCompanyForms()).ToList();
        data.Developers = page.DocumentNode.SelectNodes("//ul[@id='developerLinks']/li/a")?.Select(p => p.InnerText.HtmlDecode().TrimCompanyForms()).ToList();

        var releaseDateString = page.DocumentNode.SelectSingleNode("//dl[@class='metadata']//a[1]")?.InnerText.HtmlDecode();
        if (releaseDateString != null)
            data.ReleaseDate = ParseReleaseDate(releaseDateString);

        if (parseGenres)
        {
            SetMetadataListItems(data.Genres, page, "Genre");
            SetMetadataListItems(data.Genres, page, "Perspective");
            SetMetadataListItems(data.Genres, page, "Gameplay");
            SetMetadataListItems(data.Genres, page, "Narrative");
            SetMetadataListItems(data.Tags, page, "Visual Presentation");
            SetMetadataListItems(data.Tags, page, "Art");
            SetMetadataListItems(data.Tags, page, "Pacing");
            SetMetadataListItems(data.Tags, page, "Sport");
            SetMetadataListItems(data.Tags, page, "Vehicular");
            SetMetadataListItems(data.Tags, page, "Educational");
            SetMetadataListItems(data.Tags, page, "Interface");
            SetMetadataListItems(data.Tags, page, "Setting");
            SetMetadataListItems(data.Tags, page, "Misc");
        }

        data.Description = page.DocumentNode.SelectSingleNode("//div[@id='description-text']")?.InnerHtml.Trim();

        if (data.Description == null)
        {
            var descriptionNodes = page.DocumentNode
                .SelectSingleNode("//section[@id='gameOfficialDescription']/details")
                ?.ChildNodes.SkipWhile(node => string.IsNullOrWhiteSpace(node.OuterHtml) || node.Name == "summary")
                .Select(node => node.OuterHtml);

            if (descriptionNodes != null)
                data.Description = string.Join("", descriptionNodes).Trim();
        }

        var groups = page.DocumentNode.SelectNodes("//section[@id='gameGroups']/ul/li/a")?.Select(e => e.InnerText.HtmlDecode()).ToList() ?? new List<string>();
        foreach (string group in groups)
        {
            var target = GetGroupImportTarget(group, out string processedGroupName);
            switch (target)
            {
                case PropertyImportTarget.Genres:
                    data.Genres.Add(processedGroupName);
                    break;
                case PropertyImportTarget.Tags:
                    data.Tags.Add(processedGroupName);
                    break;
                case PropertyImportTarget.Series:
                    data.Series.Add(processedGroupName);
                    break;
                case PropertyImportTarget.Features:
                    data.Features.Add(processedGroupName);
                    break;
            }
        }

        var criticScoreString = page.DocumentNode.SelectSingleNode("//dt[text()='Critics']/following-sibling::dd")?.ChildNodes.FirstOrDefault(n => n.NodeType == HtmlNodeType.Text).InnerText.HtmlDecode().TrimEnd('%');
        if (criticScoreString != null && int.TryParse(criticScoreString, out int criticScore))
            data.CriticScore = criticScore;

        var userScoreStyle = page.DocumentNode.SelectSingleNode("//dt[text()='Players']/following-sibling::dd/span[@class='stars stars-sm'][@style]")?.GetAttributeValue("style", null);
        if (userScoreStyle != null)
        {
            foreach (var styleRule in userScoreStyle.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var styleRuleSegments = styleRule.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                if (styleRuleSegments.Count != 2)
                    continue;

                if (styleRuleSegments[0] == "--rating" && double.TryParse(styleRuleSegments[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double communityRating))
                    data.CommunityScore = (int)(communityRating * 20);
            }
        }

        var linkElements = page.DocumentNode.SelectNodes("//section[@id='gameSites' or @id='gameIdentifiers']//li/a[@href]");
        if (linkElements != null)
            data.Links.AddRange(linkElements.Select(le => new Link(le.InnerText, le.GetAttributeValue("href", ""))));

        return data;
    }

    public static ReleaseDate? ParseReleaseDate(string releaseDateString)
    {
        if (releaseDateString == null) return null;

        releaseDateString = Regex.Replace(releaseDateString, @"(?<=[0-9])(st|nd|rd|th)", "");
        if (DateTime.TryParseExact(releaseDateString, "MMMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var releaseDate))
            return new ReleaseDate(releaseDate);
        if (DateTime.TryParseExact(releaseDateString, "MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate))
            return new ReleaseDate(releaseDate.Year, releaseDate.Month);
        else if (DateTime.TryParseExact(releaseDateString, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate))
            return new ReleaseDate(releaseDate.Year);

        return null;
    }

    private static void SetMetadataListItems(ICollection<string> metadataCollection, HtmlDocument doc, string propName)
    {
        foreach (var item in GetMetadataListItems(doc, propName))
        {
            metadataCollection.Add(item.HtmlDecode());
        }
    }

    private static List<string> GetMetadataListItems(HtmlDocument doc, string propName)
    {
        var nodes = doc.DocumentNode.SelectNodes($"//dl[@class='metadata']/dt[text()='{propName}']/following-sibling::dd[1]/a");
        var output = nodes?.Select(n => n.InnerText).ToList();
        return output ?? new List<string>();
    }

    public static PropertyImportTarget GetGroupImportTarget(string groupName, out string processedGroupName)
    {
        if (!groupName.EndsWith("TV series", StringComparison.InvariantCultureIgnoreCase)
                && !groupName.StartsWith("Automobile: ", StringComparison.InvariantCultureIgnoreCase)
                && TrimEnd(groupName, out processedGroupName, " series", " licensees", " franchise"))
            return PropertyImportTarget.Series;

        if (TrimStart(groupName, out processedGroupName, "Genre: "))
            return PropertyImportTarget.Genres;

        processedGroupName = groupName;
        return PropertyImportTarget.Tags;
    }

    private static bool TrimEnd(string s, out string trimmed, params string[] remove)
    {
        trimmed = s.TrimEnd(remove);
        return trimmed.Length < s.Length;
    }

    private static bool TrimStart(string s, out string trimmed, params string[] remove)
    {
        trimmed = s.TrimStart(remove);
        return trimmed.Length < s.Length;
    }
}
