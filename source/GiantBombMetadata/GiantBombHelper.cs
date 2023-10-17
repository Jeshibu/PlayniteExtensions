using Playnite.SDK.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace GiantBombMetadata
{
    public static class GiantBombHelper
    {
        private static Regex gameIdRegex = new Regex(@"\bgiantbomb\.com/.+(?<guid>\b3030-[0-9]+\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string GetGiantBombGuidFromGameLinks(this Game game)
        {
            if (game?.Links == null)
                return null;

            foreach (var link in game.Links)
            {
                var guid = GetGiantBombGuidFromUrl(link.Url);
                if (guid != null)
                    return guid;
            }
            return null;
        }

        public static string GetGiantBombGuidFromUrl(string url)
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
            if (string.IsNullOrWhiteSpace(htmlContent) || string.IsNullOrWhiteSpace(baseUrl))
                return htmlContent;

            string[] urlAttributeNames = new[] { "href", "src" };
            var baseUri = new Uri(baseUrl);

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);
            string xpath = string.Join("|", urlAttributeNames.Select(a => $"//*[@{a}]"));
            var elements = doc.DocumentNode.SelectNodes(xpath);

            if (elements == null)
                return htmlContent;

            foreach (var el in elements)
            {
                foreach (var attrName in urlAttributeNames)
                {
                    var attribute = el.Attributes[attrName];
                    if (attribute != null)
                    {
                        if (Uri.TryCreate(baseUri, attribute.Value, out Uri newUri))
                        {
                            attribute.Value = newUri.AbsoluteUri;
                        }
                        else if (attrName == "href")
                        {
                            attribute.Value = baseUrl;
                        }

                        break;
                    }
                }
            }
            return doc.DocumentNode.OuterHtml;
        }
    }
}
