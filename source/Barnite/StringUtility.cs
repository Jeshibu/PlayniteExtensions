using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Barnite
{
    public static class StringUtility
    {
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
