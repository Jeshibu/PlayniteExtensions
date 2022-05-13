using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace PlayniteExtensions.Common
{
    public static class StringExtensions
    {
        /// <summary>
        /// Decodes HTML strings and normalizes their whitespace (no more non-breaking spaces, f.e.)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HtmlDecode(this string input)
        {
            if (input == null)
                return null;

            return Regex.Replace(HttpUtility.HtmlDecode(input), @"\s+", " ").Trim();
        }

        public static string ReplaceInvalidFileNameChars(this string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }

        public static string GetAbsoluteUrl(this string relativeUrl, string currentUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return null;

            var baseUri = new Uri(currentUrl);
            var absoluteUri = new Uri(baseUri, relativeUrl);
            return absoluteUri.AbsoluteUri;
        }

        public static string TrimStart(this string s, string remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return TrimStart(s, new[] { remove }, stringComparison);
        }

        public static string TrimStart(this string s, string[] remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            if (s == null)
                return s;

            foreach (var r in remove)
            {
                if (s.StartsWith(r, stringComparison))
                    s = s.Substring(r.Length);
            }
            return s;
        }

        public static string TrimEnd(this string s, string remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return TrimEnd(s, new[] { remove }, stringComparison);
        }

        public static string TrimEnd(this string s, string[] remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            if (s == null)
                return s;

            foreach (var r in remove)
            {
                if (s.EndsWith(r, stringComparison))
                    s = s.Remove(s.Length - r.Length);
            }
            return s;
        }

        private static Regex CompanyFormRegex = new Regex(@",?\s+((co[,.\s]+)ltd|(l\.)inc|s\.?l|a\.?s|limited|l\.?l\.?c|s\.?a\.?r\.?l|s\.?r\.?o|gmbh)\.?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string TrimCompanyForms(this string s)
        {
            return CompanyFormRegex.Replace(s, string.Empty);
        }
    }
}
