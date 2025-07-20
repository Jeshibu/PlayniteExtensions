using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace SteamTagsImporter;

public class SteamTagScraper(Func<string, string, SteamTagScraper.Delistable<string>> getSteamStorePageHtmlMethod) : ISteamTagScraper
{
    private static readonly Regex TagJsonRegex = new(@"InitAppTagModal\(\s*\d+,\s*(?<json>\[[^\]]+\])", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    public Func<string, string, Delistable<string>> GetSteamStorePageHtmlMethod { get; } = getSteamStorePageHtmlMethod;

    public SteamTagScraper()
        : this(GetSteamStorePageHtmlDefault)
    {
    }

    public Delistable<IEnumerable<SteamTag>> GetTags(string appId, string languageKey = null)
    {
        var html = GetSteamStorePageHtmlMethod(appId, languageKey);

        var match = TagJsonRegex.Match(html.Value);
        if (!match.Success)
            return new Delistable<IEnumerable<SteamTag>>([], html.Delisted);

        var json = match.Groups["json"].Value;
        var steamTags = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SteamTag>>(json);
        foreach (var tag in steamTags)
        {
            tag.Name = tag.Name.Trim(); //Trimmed because at least one tag was found to have a space at the end ("Dystopian ")
        }
        return new Delistable<IEnumerable<SteamTag>>(steamTags, html.Delisted);
    }

    private static Delistable<string> GetSteamStorePageHtmlDefault(string appId, string languageKey = null)
    {
        var request = (HttpWebRequest)WebRequest.Create($"https://store.steampowered.com/app/{appId}/");
        var cookies = new CookieContainer(2);
        cookies.Add(new Cookie("wants_mature_content", "1", "/app/" + appId, "store.steampowered.com"));
        cookies.Add(new Cookie("birthtime", "628495201", "/", "store.steampowered.com"));
        if (languageKey != null)
            cookies.Add(new Cookie("Steam_Language", languageKey, "/", "store.steampowered.com"));
        request.CookieContainer = cookies;
        request.Timeout = 15000;
        using (var response = request.GetResponse())
        using (var responseStream = response.GetResponseStream())
        using (var streamReader = new StreamReader(responseStream))
        {
            bool delisted = response.ResponseUri?.ToString() == "https://store.steampowered.com/";
            string html = streamReader.ReadToEnd();
            return new Delistable<string>(html, delisted);
        }
    }

    public class Delistable<T>(T value, bool delisted)
    {
        public bool Delisted { get; set; } = delisted;
        public T Value { get; set; } = value;
    }
}

public class SteamTag
{
    public int TagId { get; set; }
    public string Name { get; set; }
}
