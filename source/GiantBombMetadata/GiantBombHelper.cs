using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GiantBombMetadata
{
    public static class GiantBombHelper
    {
        private static Regex gameIdRegex = new Regex(@"[/.]giantbomb\.com\b.+(?<guid>\b3030-[0-9]+\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string GetGiantBombGuidFromGameLinks(this Game game)
        {
            if (game?.Links == null)
                return null;

            foreach (var link in game.Links)
            {
                var guid = GetGiantBomgGuidFromUrl(link.Url);
                if (guid != null)
                    return guid;
            }
            return null;
        }

        public static string GetGiantBomgGuidFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var match = gameIdRegex.Match(url);
            if (match.Success)
                return match.Groups["guid"].Value;
            else
                return null;
        }

        public static string MakeHtmlUrlsAbsolute(string htmlContent, string baseUrl)
        {
            string[] urlAttributeNames = new[] { "href", "src" };
            var baseUri = new Uri(baseUrl);

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);
            string xpath = string.Join("|", urlAttributeNames.Select(a => $"//*[@{a}]"));
            var elements = doc.DocumentNode.SelectNodes(xpath);
            foreach (var el in elements)
            {
                foreach (var attrName in urlAttributeNames)
                {
                    var attribute = el.Attributes[attrName];
                    if (attribute != null)
                    {
                        attribute.Value = new Uri(baseUri, attribute.Value).AbsoluteUri;
                        break;
                    }
                }
            }
            return doc.DocumentNode.OuterHtml;
        }
    }
}
