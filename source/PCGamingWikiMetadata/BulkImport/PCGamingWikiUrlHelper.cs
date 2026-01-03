using System.Net;
using System.Text;

namespace PCGamingWikiBulkImport;

public static class PCGamingWikiUrlHelper
{
    extension(string title)
    {
        public string TitleToSlug(bool urlEncode = true)
        {
            if (string.IsNullOrWhiteSpace(title))
                return title;

            var sb = new StringBuilder();
            foreach (char c in title)
                sb.Append(EscapeSlugCharacter(c, urlEncode));

            return sb.ToString();
        }

        public string SlugToUrl()
        {
            return $"https://www.pcgamingwiki.com/wiki/{title}";
        }
    }

    private static string EscapeSlugCharacter(char c, bool urlEncode)
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
                if (urlEncode)
                    return WebUtility.UrlEncode(c.ToString());
                else
                    return c.ToString();
        }
        ;
    }
}
