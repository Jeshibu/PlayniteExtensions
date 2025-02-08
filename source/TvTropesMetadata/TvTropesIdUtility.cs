using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;

namespace TvTropesMetadata
{
    public class TvTropesIdUtility : SingleExternalDatabaseIdUtility
    {
        public override ExternalDatabase Database { get; } = ExternalDatabase.TvTropes;

        public override IEnumerable<Guid> LibraryIds { get; } = new Guid[0];

        public override DbId GetIdFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return default;

            var trimmed = url.TrimStart("https://tvtropes.org/pmwiki/pmwiki.php/");
            if (trimmed != url)
                return DbId.TvTropes(trimmed);
            else
                return default;
        }
    }
}
