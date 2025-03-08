using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rawg.Common
{
    public class RawgIdUtility : SingleExternalDatabaseIdUtility
    {
        private static Regex rawgGameUrlRegex = new Regex(@"^https://rawg\.io/games/(?<id>[0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override ExternalDatabase Database { get; } = ExternalDatabase.RAWG;

        public override IEnumerable<Guid> LibraryIds { get; } = new[] { RawgMetadataHelper.RawgLibraryId };

        public override DbId GetIdFromUrl(string url)
        {
            var match = rawgGameUrlRegex.Match(url);
            if (!match.Success)
                return default;

            string id = match.Groups["id"].Value;
            return new DbId(ExternalDatabase.RAWG, id);
        }
    }
}