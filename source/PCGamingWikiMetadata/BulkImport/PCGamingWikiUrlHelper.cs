using System.Net;

namespace PCGamingWikiBulkImport
{
    internal static class PCGamingWikiUrlHelper
    {
        public static string TitleToSlug(string title)
        {
            return WebUtility.UrlEncode(title);
        }

        public static string SlugToUrl(string slug)
        {
            return $"https://www.pcgamingwiki.com/wiki/{slug}";
        }
    }
}
