using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace PCGamingWikiBulkImport
{
    public class PCGamingWikiIdUtility : SingleExternalDatabaseIdUtility
    {
        private readonly Regex PCGamingWikiUrlRegex = new Regex(@"^https?://(www\.)?pcgamingwiki\.com/(api/appid\.php\?appid=(?<steamId>[0-9]+)|wiki/(?<slug>[^?#]+))", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        public override ExternalDatabase Database { get; } = ExternalDatabase.PCGamingWiki;

        public override IEnumerable<Guid> LibraryIds { get; } = new Guid[0];

        public override DbId GetIdFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return default;

            var match = PCGamingWikiUrlRegex.Match(url);
            if (!match.Success)
                return default;

            var slugGroup = match.Groups["slug"];
            if (slugGroup.Success)
                return DbId.PCGW(SlugToId(slugGroup.Value));

            var steamIdGroup = match.Groups["steamId"];
            if (steamIdGroup.Success)
                return DbId.Steam(steamIdGroup.Value);

            return default;
        }

        /// <summary>
        /// Convert a wiki URL slug to a universal ID - used to convert old format PCGW slugs that were just the titles URL escaped to something equatable to the current format
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        public static string SlugToId(string slug) => WebUtility.UrlDecode(slug).Replace(' ', '_');
    }
}
