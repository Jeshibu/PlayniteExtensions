using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;

namespace TvTropesMetadata
{
    public class TvTropesIdUtility : SingleExternalDatabaseIdUtility
    {
        public override ExternalDatabase Database { get; } = ExternalDatabase.TvTropes;

        public override IEnumerable<Guid> LibraryIds { get; } = new Guid[0];

        public override (ExternalDatabase Database, string Id) GetIdFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return (ExternalDatabase.None, null);

            var trimmed = url.TrimStart("https://tvtropes.org/pmwiki/pmwiki.php/");
            if (trimmed != url)
                return (ExternalDatabase.TvTropes, trimmed);
            else
                return (ExternalDatabase.None, null);
        }
    }
}
