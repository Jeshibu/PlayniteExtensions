using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using System;
using System.Linq;

namespace PlayniteExtensions.Common
{
    public static class AngleSharpStringExtensions
    {
        public static string MakeHtmlUrlsAbsolute(this string htmlContent, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(htmlContent) || string.IsNullOrWhiteSpace(baseUrl))
                return htmlContent;

            var doc = new HtmlParser().Parse(htmlContent);
            return MakeHtmlUrlsAbsolute(doc, baseUrl)?.Body.InnerHtml;
        }

        public static IHtmlDocument MakeHtmlUrlsAbsolute(this IHtmlDocument doc, string baseUrl)
        {
            string[] urlAttributeNames = new[] { "href", "src" };
            var baseUri = new Uri(baseUrl);

            string selector = string.Join(",", urlAttributeNames.Select(a => $"[{a}]"));
            var elements = doc.QuerySelectorAll(selector);

            if (elements == null)
                return doc;

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
            return doc;
        }

    }
}
