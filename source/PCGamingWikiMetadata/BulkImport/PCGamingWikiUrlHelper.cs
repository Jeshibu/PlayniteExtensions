using System.Net;
using System.Text;

namespace PCGamingWikiBulkImport
{
    public static class PCGamingWikiUrlHelper
    {
        public static string TitleToSlug(this string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return title;

            var sb = new StringBuilder();
            foreach (char c in title)
                sb.Append(EscapeSlugCharacter(c));

            return sb.ToString();
        }

        public static string SlugToUrl(this string slug)
        {
            return $"https://www.pcgamingwiki.com/wiki/{slug}";
        }

        private static string EscapeSlugCharacter(char c)
        {
            if (char.IsLetterOrDigit(c))
                return c.ToString();

            switch (c)
            {
                case ' ':
                    return "_";
                case ':':
                case '-':
                case '.':
                case '/':
                case '~':
                case ';':
                case ',':
                case '\'':
                    return c.ToString();
                default:
                    return WebUtility.UrlEncode(c.ToString());
            };
        }
    }
}
