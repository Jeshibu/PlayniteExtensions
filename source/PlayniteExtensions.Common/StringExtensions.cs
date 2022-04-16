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
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
