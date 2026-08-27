using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rawg.Common;

public class RawgIdUtility : SingleExternalDatabaseIdUtilityWithRegexUrlMatching
{
    public override Regex UrlRegex { get; } = new(@"^https://rawg\.io/games/(?<id>[0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override ExternalDatabase Database => ExternalDatabase.RAWG;

    public override IEnumerable<Guid> LibraryIds { get; } = [RawgMetadataHelper.RawgLibraryId];
}
