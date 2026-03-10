using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IgnMetadata;

public abstract class SimpleWebsiteIdUtility(string urlBase, ExternalDatabase db) : SingleExternalDatabaseIdUtility
{
    public override ExternalDatabase Database => db;
    public override IEnumerable<Guid> LibraryIds => [];

    public override DbId GetIdFromUrl(string url)
    {
        if (url == null || !url.StartsWith(urlBase))
            return default;

        string id = new(url.Skip(urlBase.Length).TakeWhile(c => c != '?' && c != '#').ToArray());
        return new DbId(db, id);
    }

    public string GetUrlFromId(string id) => urlBase + id;
}

public class IgnIdUtility() : SimpleWebsiteIdUtility("https://www.ign.com/games/", ExternalDatabase.IGN);

public class HltbIdUtility() : SimpleWebsiteIdUtility("https://howlongtobeat.com/game/", ExternalDatabase.HowLongToBeat)
{
    private Regex oldUrlFormatRegex = new(@"^https://howlongtobeat\.com/game\?id=(?<id>[0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override DbId GetIdFromUrl(string url)
    {
        var normalId = base.GetIdFromUrl(url);
        if (normalId != default || url == null)
            return normalId;

        var oldFormatMatch = oldUrlFormatRegex.Match(url);
        if (oldFormatMatch.Success)
            return new DbId(ExternalDatabase.HowLongToBeat, oldFormatMatch.Groups["id"].Value);

        return default;
    }
}
