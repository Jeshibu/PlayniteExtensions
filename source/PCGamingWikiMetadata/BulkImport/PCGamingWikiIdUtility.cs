using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PCGamingWikiBulkImport
{
    public class PCGamingWikiIdUtility : SingleExternalDatabaseIdUtility
    {
        private readonly Regex PCGamingWikiUrlRegex = new Regex(@"^https?://(www\.)?pcgamingwiki\.com/(api/appid\.php\?appid=(?<steamId>[0-9]+)|wiki/(?<title>[^?#]+))", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        public override ExternalDatabase Database { get; } = ExternalDatabase.PCGamingWiki;

        public override IEnumerable<Guid> LibraryIds { get; } = new Guid[0];

        public override (ExternalDatabase Database, string Id) GetIdFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return (ExternalDatabase.None, null);

            var match = PCGamingWikiUrlRegex.Match(url);
            if (!match.Success)
                return (ExternalDatabase.None, null);

            var titleGroup = match.Groups["title"];
            if (titleGroup.Success)
                return (ExternalDatabase.PCGamingWiki, titleGroup.Value);

            var steamIdGroup = match.Groups["steamId"];
            if (steamIdGroup.Success)
                return (ExternalDatabase.Steam, steamIdGroup.Value);

            return (ExternalDatabase.None, null);
        }
    }
}
