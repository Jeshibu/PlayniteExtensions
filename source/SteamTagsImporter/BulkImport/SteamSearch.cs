using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SteamTagsImporter.BulkImport;

public class SteamSearch(IWebDownloader downloader, SteamTagsImporterSettings settings)
{
    private readonly HtmlParser htmlParser = new();
    private readonly string[] NotUserSelectableParams = ["specials", "hidef2p", "category1", "supportedlang", "os"];

    private static string GetCategory(string param) => param switch
    {
        "specials" => "On Sale",
        "hidef2p" => "Hide Free To Play Games",
        "category1" => "Type",
        "category2" => "Feature",
        "category3" => "Number of players",
        "controllersupport" => "Controller support",
        "deck_compatibility" => "Steam Deck compatibility",
        "vrsupport" => "VR support",
        "os" => "Operating System",
        "tags" => "Tags",
        "supportedlang" => "Supported Language",
        "accessibility" => "Accessibility",
        _ => param,
    };

    public IEnumerable<SteamProperty> GetProperties()
    {
        //category2=35 here because that's the In-App Purchases feature, which is normally hidden unless already part of the filter like this
        var source = downloader.DownloadString($"https://store.steampowered.com/search/?category2=35&l={settings.LanguageKey}").ResponseContent;
        var doc = new HtmlParser().Parse(source);
        var rows = doc.QuerySelectorAll(".tab_filter_control_row[data-param][data-value][data-loc]");
        foreach (var row in rows)
        {
            var prop = new SteamProperty
            {
                Name = row.GetAttribute("data-loc"),
                Param = row.GetAttribute("data-param"),
                Value = row.GetAttribute("data-value"),
            };

            prop.Category = GetCategory(prop.Param);

            if (NotUserSelectableParams.Contains(prop.Param))
                continue;

            yield return prop;
        }
    }

    public string GetSearchRequestUrl(string param, string value, int start)
    {
        var url = $"https://store.steampowered.com/search/results/?query=&start={start}&count=50&dynamic_data=&force_infinite=1&category1=998,994,992,997&{param}={value}&ndl=1&snr=1_7_7_230_7&infinite=1";
        if (settings.OnlyImportGamesWithThisLanguageSupport)
            url += $"&language={settings.LanguageKey}";

        return url;
    }

    public SteamSearchResponse SearchGames(string param, string value, int start)
    {
        var url = GetSearchRequestUrl(param, value, start);
        var response = downloader.DownloadString(url);
        return JsonConvert.DeserializeObject<SteamSearchResponse>(response.ResponseContent);
    }

    public IEnumerable<GameDetails> ParseSearchResultHtml(string html)
    {
        var doc = htmlParser.Parse(html);
        var links = doc.QuerySelectorAll("a[href][data-ds-appid]");
        foreach (var a in links)
        {
            var gd = new GameDetails
            {
                Names = [a.QuerySelector(".title").TextContent.HtmlDecode()],
                Url = a.GetAttribute("href").Split('?').First(),
                ReleaseDate = ParseReleaseDate(a.QuerySelector("search_released")?.TextContent.HtmlDecode())
            };

            if (settings.LimitTaggingToPcGames)
            {
                var platformImgClassName = "platform_img";
                var platformElements = a.GetElementsByClassName(platformImgClassName);
                foreach (var pe in platformElements)
                {
                    var platformClass = pe.ClassList.FirstOrDefault(c => c != platformImgClassName);
                    if (platformClass != null)
                        gd.Platforms.Add(new MetadataNameProperty(platformClass));
                }
            }

            yield return gd;
        }
    }

    public static ReleaseDate? ParseReleaseDate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (DateTime.TryParseExact(input, "d MMM, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
            return new ReleaseDate(date.Year, date.Month, date.Day);

        if (DateTime.TryParseExact(input, "MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
            return new ReleaseDate(date.Year, date.Month);

        var match = QuarterReleaseDate.Match(input);
        if (match.Success)
        {
            var year = int.Parse(match.Groups["year"].Value);
            var quarterGroup = match.Groups["quarter"];
            if (quarterGroup != null && quarterGroup.Success && int.TryParse(quarterGroup.Value, out int quarter))
                return new ReleaseDate(year, quarter * 3);

            return new ReleaseDate(year);
        }

        return null;
    }

    private static Regex QuarterReleaseDate = new(@"^(Q(?<quarter>[1-4]) )?(?<year>[0-9]{4})$", RegexOptions.Compiled);
}
