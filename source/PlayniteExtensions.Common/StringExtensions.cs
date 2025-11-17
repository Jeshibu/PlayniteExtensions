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
    /// <summary>
    /// Decodes HTML strings and normalizes their whitespace (no more non-breaking spaces, f.e.)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string HtmlDecode(this string input)
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
        return TrimStart(s, [remove], stringComparison);
    }

    public static string TrimStart(this string s, IEnumerable<string> remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        if (s == null || remove == null)
            return s;

        foreach (var r in remove)
        {
            if (r != null && s.StartsWith(r, stringComparison))
                s = s.Substring(r.Length);
        }

        return s;
    }

    public static string TrimEnd(this string s, string remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        return TrimEnd(s, [remove], stringComparison);
    }

    public static string TrimEnd(this string s, IEnumerable<string> remove, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
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

    private static readonly Regex CompanyFormRegex = new(@",?\s+((co[,.\s]+)?ltd|(l\.)?inc|s\.?l|a[./]?s|limited|l\.?l\.?(c|p)|s\.?a(\.?r\.?l)?|s\.?r\.?o|gmbh|ab)\.?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string TrimCompanyForms(this string s)
    {
        return CompanyFormRegex.Replace(s, string.Empty).Trim();
    }

    public static IEnumerable<string> SplitCompanies(this string s)
    {
        var splitRegex = new Regex(@",\s+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        return splitRegex.Split(s.TrimCompanyForms()).Select(TrimCompanyForms);
    }

    private static readonly Regex deflateRegex = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);

    /// <summary>
    /// Remove all characters except letters and numbers. Useful to compare game titles like "S.T.A.L.K.E.R. - Call of Pripyat" and "STALKER: Call of Pripyat"
    /// </summary>
    /// <param name="gameName"></param>
    /// <returns></returns>
    public static string Deflate(this string gameName)
    {
        if (string.IsNullOrEmpty(gameName))
            return string.Empty;

        return deflateRegex.Replace(gameName, string.Empty);
    }

    /// <summary>
    /// Parse a release date in the yyyy-MM-dd or yyyy-MM or yyyy formats
    /// </summary>
    /// <param name="dateString"></param>
    /// <returns></returns>
    public static ReleaseDate? ParseReleaseDate(this string dateString) => ParseReleaseDate(dateString, null);


    /// <summary>
    /// Parse a release date in the yyyy-MM-dd or yyyy-MM or yyyy formats
    /// </summary>
    /// <param name="dateString"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static ReleaseDate? ParseReleaseDate(this string dateString, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        var segments = dateString.Split('-');
        List<int> numberSegments;
        try
        {
            numberSegments = segments.Select(int.Parse).ToList();
        }
        catch (Exception ex)
        {
            logger?.Warn(ex, $"Could not parse release date {dateString}");
            return null;
        }

        switch (numberSegments.Count)
        {
            case 1:
                return new ReleaseDate(numberSegments[0]);
            case 2:
                return new ReleaseDate(numberSegments[0], numberSegments[1]);
            case 3:
                return new ReleaseDate(numberSegments[0], numberSegments[1], numberSegments[2]);
            default:
                logger?.Warn($"Could not parse release date {dateString}");
                return null;
        }
    }

    public static bool Contains(this string str, string value, StringComparison comparisonType)
    {
        return str?.IndexOf(value, 0, comparisonType) != -1;
    }

    private static readonly Regex installSizeRegex = new(@"\b(?<number>[0-9.]+)\s+(?<scale>[KMGT]i?B)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static ulong? ParseInstallSize(this string str, CultureInfo culture = null)
    {
        var match = installSizeRegex.Match(str);
        if (!match.Success)
            return null;

        string number = match.Groups["number"].Value;
        string scale = match.Groups["scale"].Value.ToUpperInvariant();

        culture ??= CultureInfo.InvariantCulture;
        if (!double.TryParse(number, NumberStyles.Number | NumberStyles.AllowDecimalPoint, culture, out double n))
            return null;

        int? power = scale switch
        {
            "KB" or "KiB" => 1,
            "MB" or "MiB" => 2,
            "GB" or "GiB" => 3,
            "TB" or "TiB" => 4,
            _ => null
        };
        if (power == null)
            return null;

        var output = Convert.ToUInt64(n * Math.Pow(1024, power.Value));
        return output;
    }
}
