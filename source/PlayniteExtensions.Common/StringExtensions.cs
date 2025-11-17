using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace PlayniteExtensions.Common;

public static class StringExtensions
{
    private static readonly Regex CompanyFormRegex = new(@",?\s+((co[,.\s]+)?ltd|(l\.)?inc|s\.?l|a[./]?s|limited|l\.?l\.?(c|p)|s\.?a(\.?r\.?l)?|s\.?r\.?o|gmbh|ab)\.?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DeflateRegex = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);
    private static readonly Regex InstallSizeRegex = new(@"\b(?<number>[0-9.]+)\s+(?<scale>[KMGT]i?B)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <param name="input"></param>
    extension(string input)
    {
        /// <summary>
        /// Decodes HTML strings and normalizes their whitespace (no more non-breaking spaces, f.e.)
        /// </summary>
        /// <returns></returns>
        public string HtmlDecode()
        {
            if (input == null)
                return null;

            return Regex.Replace(WebUtility.HtmlDecode(input), @"\s+", match =>
            {
                if (match.Value.Contains('\n'))
                    return Environment.NewLine;
                else
                    return " ";
            }).Trim();
        }

        public string ReplaceInvalidFileNameChars()
        {
            return string.Join("_", input.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }

        public string GetAbsoluteUrl(string currentUrl)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var baseUri = new Uri(currentUrl);
            var absoluteUri = new Uri(baseUri, input);
            return absoluteUri.AbsoluteUri;
        }

        public string TrimStart(string remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return TrimStart(input, [remove], stringComparison);
        }

        public string TrimStart(IEnumerable<string> remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            if (input == null || remove == null)
                return input;

            foreach (var r in remove)
            {
                if (r != null && input.StartsWith(r, stringComparison))
                    input = input.Substring(r.Length);
            }

            return input;
        }

        public string TrimEnd(string remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return TrimEnd(input, [remove], stringComparison);
        }

        public string TrimEnd(IEnumerable<string> remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            if (input == null)
                return input;

            foreach (var r in remove)
            {
                if (input.EndsWith(r, stringComparison))
                    input = input.Remove(input.Length - r.Length);
            }

            return input;
        }

        public string TrimCompanyForms()
        {
            return CompanyFormRegex.Replace(input, string.Empty).Trim();
        }

        public IEnumerable<string> SplitCompanies()
        {
            var splitRegex = new Regex(@",\s+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            return splitRegex.Split(input.TrimCompanyForms()).Select(TrimCompanyForms);
        }

        /// <summary>
        /// Remove all characters except letters and numbers. Useful to compare game titles like "S.T.A.L.K.E.R. - Call of Pripyat" and "STALKER: Call of Pripyat"
        /// </summary>
        /// <returns></returns>
        public string Deflate()
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return DeflateRegex.Replace(input, string.Empty);
        }

        /// <summary>
        /// Parse a release date in the yyyy-MM-dd or yyyy-MM or yyyy formats
        /// </summary>
        /// <returns></returns>
        public ReleaseDate? ParseReleaseDate() => ParseReleaseDate(input, null);

        /// <summary>
        /// Parse a release date in the yyyy-MM-dd or yyyy-MM or yyyy formats
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public ReleaseDate? ParseReleaseDate(ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var segments = input.Split('-');
            List<int> numberSegments;
            try
            {
                numberSegments = segments.Select(int.Parse).ToList();
            }
            catch (Exception ex)
            {
                logger?.Warn(ex, $"Could not parse release date {input}");
                return null;
            }

            switch (numberSegments.Count)
            {
                case 1: return new ReleaseDate(numberSegments[0]);
                case 2: return new ReleaseDate(numberSegments[0], numberSegments[1]);
                case 3: return new ReleaseDate(numberSegments[0], numberSegments[1], numberSegments[2]);
                default:
                    logger?.Warn($"Could not parse release date {input}");
                    return null;
            }
        }

        public bool Contains(string value, StringComparison comparisonType)
        {
            return input?.IndexOf(value, 0, comparisonType) != -1;
        }

        public ulong? ParseInstallSize(CultureInfo culture = null)
        {
            var match = InstallSizeRegex.Match(input);
            if (!match.Success)
                return null;

            string number = match.Groups["number"].Value;
            string scale = match.Groups["scale"].Value.ToUpperInvariant();

            culture ??= CultureInfo.InvariantCulture;
            if (!double.TryParse(number, NumberStyles.Number | NumberStyles.AllowDecimalPoint, culture, out double n))
                return null;

            int? power = scale[0] switch
            {
                'K' => 1,
                'M' => 2,
                'G' => 3,
                'T' => 4,
                _ => null
            };
            if (power == null)
                return null;

            var output = Convert.ToUInt64(n * Math.Pow(1024, power.Value));
            return output;
        }
    }
}
