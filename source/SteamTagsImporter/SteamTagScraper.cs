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

        private class JsonSteamTag
        {
            public int tagid { get; set; }
            public string name { get; set; }
            public int count { get; set; }
            public bool browseable { get; set; }
        }

        public IEnumerable<string> GetTags(string appId)
        {
            var html = GetSteamStorePageHtml(appId);

            var match = TagJsonRegex.Match(html);
            if (!match.Success)
                return new string[0];

            var json = match.Groups["json"].Value;
            var steamTags = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonSteamTag>>(json);
            return steamTags.Select(t => t.name);
        }

        private static string GetSteamStorePageHtml(string appId)
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
                string html = streamReader.ReadToEnd();
                return html;
            }
        }
    }
}
