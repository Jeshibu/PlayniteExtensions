using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SteamTagsImporter
{
    public class SteamTagScraper : ISteamTagScraper
    {
        private static readonly Regex TagJsonRegex = new Regex(@"InitAppTagModal\(\s*\d+,\s*(?<json>\[[^\]]+\])", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public Func<string, Delistable<string>> GetSteamStorePageHtmlMethod { get; }

        public SteamTagScraper()
            : this(GetSteamStorePageHtmlDefault)
        {
        }

        public SteamTagScraper(Func<string, Delistable<string>> getSteamStorePageHtmlMethod)
        {
            GetSteamStorePageHtmlMethod = getSteamStorePageHtmlMethod;
        }

        private class JsonSteamTag
        {
            public int tagid { get; set; }
            public string name { get; set; }
            public int count { get; set; }
            public bool browseable { get; set; }
        }

        public Delistable<IEnumerable<string>> GetTags(string appId)
        {
            var html = GetSteamStorePageHtmlMethod(appId);

            var match = TagJsonRegex.Match(html.Value);
            if (!match.Success)
                return new Delistable<IEnumerable<string>>(new string[0], html.Delisted);

            var json = match.Groups["json"].Value;
            var steamTags = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonSteamTag>>(json);
            var outputTags = steamTags.Select(t => t.name.Trim()); //Trimmed because at least one tag was found to have a space at the end ("Dystopian ")
            return new Delistable<IEnumerable<string>>(outputTags, html.Delisted);
        }

        private static Delistable<string> GetSteamStorePageHtmlDefault(string appId)
        {
            var request = (HttpWebRequest)WebRequest.Create($"https://store.steampowered.com/app/{appId}/");
            var cookies = new CookieContainer(2);
            cookies.Add(new Cookie("wants_mature_content", "1", "/app/" + appId, "store.steampowered.com"));
            cookies.Add(new Cookie("birthtime", "628495201", "/", "store.steampowered.com"));
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

        public class Delistable<T>
        {
            public bool Delisted { get; set; }
            public T Value { get; set; }

            public Delistable(T value, bool delisted)
            {
                Value = value;
                Delisted = delisted;
            }
        }
    }
}
