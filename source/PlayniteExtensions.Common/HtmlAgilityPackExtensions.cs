using HtmlAgilityPack;
using System.Linq;
using System;

namespace PlayniteExtensions.Common
{
    public static class HtmlAgilityPackExtensions
    {
        public static string MakeHtmlUrlsAbsolute(this string htmlContent, string baseUrl)
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