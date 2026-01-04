using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace PlayniteExtensions.Common;

public enum ExternalDatabase
{
    None,
    Steam,
    GOG,
    PCGamingWiki,
    MobyGames,
    GiantBomb,
    TvTropes,
    RAWG,
    Wikipedia,
    // if you add a 16 value here, update the bit shift in DbId.GetHashCode() to 5 instead of 4
}

public readonly struct DbId(ExternalDatabase database, string id) : IEquatable<DbId>
{
    public readonly ExternalDatabase Database = database;
    public readonly string Id = id?.ToLowerInvariant();

    public override bool Equals(object obj) => obj is DbId otherId && this == otherId;
    public bool Equals(DbId other) => this == other;
    public static bool operator ==(DbId left, DbId right) => left.Database == right.Database && left.Id == right.Id;
    public static bool operator !=(DbId left, DbId right) => !(left == right);
    public override int GetHashCode() => (int)Database ^ Id.GetHashCode() << 4;

    public static DbId NoDb(string id) => new(ExternalDatabase.None, id);
    public static DbId Steam(string id) => new(ExternalDatabase.Steam, id);
    public static DbId GOG(string id) => new(ExternalDatabase.GOG, id);
    public static DbId PCGW(string id) => new(ExternalDatabase.PCGamingWiki, id);
    public static DbId Moby(string id) => new(ExternalDatabase.MobyGames, id);
    public static DbId GiantBomb(string id) => new(ExternalDatabase.GiantBomb, id);
    public static DbId TvTropes(string id) => new(ExternalDatabase.TvTropes, id);
    public static DbId RAWG(string id) => new(ExternalDatabase.RAWG, id);
    public static DbId Wikipedia(string id) => new(ExternalDatabase.Wikipedia, id);

}

public interface IExternalDatabaseIdUtility
{
    IEnumerable<ExternalDatabase> Databases { get; }
    DbId GetIdFromUrl(string url);
    ExternalDatabase GetDatabaseFromPluginId(Guid libraryId);
    IEnumerable<DbId> GetIdsFromGame(Game game);
}

public interface ISingleExternalDatabaseIdUtility : IExternalDatabaseIdUtility
{
    ExternalDatabase Database { get; }
    IEnumerable<Guid> LibraryIds { get; }
}

public abstract class SingleExternalDatabaseIdUtility : ISingleExternalDatabaseIdUtility
{
    public IEnumerable<ExternalDatabase> Databases => [Database];

    public abstract ExternalDatabase Database { get; }

    public abstract IEnumerable<Guid> LibraryIds { get; }

    public ExternalDatabase GetDatabaseFromPluginId(Guid pluginId)
    {
        if (LibraryIds.Contains(pluginId))
            return Database;

        return ExternalDatabase.None;
    }

    public abstract DbId GetIdFromUrl(string url);

    public IEnumerable<DbId> GetIdsFromGame(Game game)
    {
        if (LibraryIds.Contains(game.PluginId))
            yield return new DbId(Database, game.GameId);

        var linkIds = game.Links?.Select(l => GetIdFromUrl(l?.Url)).Where(id => id.Database != ExternalDatabase.None);
        if (linkIds != null)
            foreach (var linkId in linkIds)
                yield return linkId;
    }
}

public class SteamIdUtility : SingleExternalDatabaseIdUtility
{
    public readonly Regex SteamUrlRegex = new(@"^(steam://openurl/)?https?://(store\.steampowered\.com|steamcommunity\.com|steamdb\.info)/app/(?<id>[0-9]+)",
                                               RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

    public override ExternalDatabase Database => ExternalDatabase.Steam;

    public override IEnumerable<Guid> LibraryIds { get; } = [Guid.Parse("CB91DFC9-B977-43BF-8E70-55F46E410FAB")];

    public override DbId GetIdFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return default;

        var match = SteamUrlRegex.Match(url);
        if (match.Success)
            return DbId.Steam(match.Groups["id"].Value);

        return default;
    }
}

public class GOGIdUtility : SingleExternalDatabaseIdUtility
{
    private readonly Regex GOGUrlRegex = new(@"^https://www\.gogdb\.org/product/(?<id>[0-9]+)");

    public override ExternalDatabase Database => ExternalDatabase.GOG;

    public override IEnumerable<Guid> LibraryIds => [
        Guid.Parse("AEBE8B7C-6DC3-4A66-AF31-E7375C6B5E9E"), // GOG
        Guid.Parse("03689811-3F33-4DFB-A121-2EE168FB9A5C"), // GOG OSS
    ];

    public override DbId GetIdFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return default;

        var match = GOGUrlRegex.Match(url);
        if (match.Success)
            return DbId.GOG(match.Groups["id"].Value);

        return default;
    }
}

public class MobyGamesIdUtility : SingleExternalDatabaseIdUtility
{
    private readonly Regex UrlIdRegex = new(@"\bmobygames\.com/game/(?<id>[0-9]+)(/|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public override ExternalDatabase Database => ExternalDatabase.MobyGames;

    public override IEnumerable<Guid> LibraryIds { get; } = [];

    public override DbId GetIdFromUrl(string url)
    {
        if (url == null) return default;

        var match = UrlIdRegex.Match(url);
        if (!match.Success) return default;
        var idString = match.Groups["id"].Value;
        return DbId.Moby(idString);
    }
}

public class WikipediaIdUtility : SingleExternalDatabaseIdUtility
{
    private readonly Regex idRegex = new(@"https?://(?<lang>[a-z]+)\.wikipedia\.org/wiki/(?<article>[^?]+)", RegexOptions.Compiled);

    public override ExternalDatabase Database => ExternalDatabase.Wikipedia;
    public override IEnumerable<Guid> LibraryIds => [];
    public override DbId GetIdFromUrl(string url)
    {
        if(string.IsNullOrWhiteSpace(url))
            return default;

        var match = idRegex.Match(url);
        if(!match.Success)
            return new(ExternalDatabase.None, null);

        var idString = WebUtility.UrlDecode(match.Groups["article"].Value);
        var lang = match.Groups["lang"].Value;
        return DbId.Wikipedia($"{lang}/{idString}");
    }

    public static Tuple<string, string> GetLanguageAndIdFromDbId(DbId dbId)
    {
        if (dbId.Database != ExternalDatabase.Wikipedia)
            return null;

        var split = dbId.Id.Split(['/'], 2);
        return new(split[0], split[1]);
    }

    public static string ToWikipediaUrl(DbId dbId)
    {
        var langAndId = GetLanguageAndIdFromDbId(dbId);
        if (langAndId == null)
            return null;

        return ToWikipediaUrl(langAndId.Item1, langAndId.Item2);
    }

    public static string ToWikipediaUrl(string lang, string name) => $"https://{lang}.wikipedia.org/wiki/{name?.Replace(' ', '_')}";
}

public class AggregateExternalDatabaseUtility : IExternalDatabaseIdUtility
{
    private readonly List<ISingleExternalDatabaseIdUtility> databaseIdUtilities = [];

    public IEnumerable<ExternalDatabase> Databases => databaseIdUtilities.SelectMany(x => x.Databases).Distinct();

    public AggregateExternalDatabaseUtility(params ISingleExternalDatabaseIdUtility[] dbIdUtilities)
    {
        databaseIdUtilities.AddRange(dbIdUtilities);
    }

    public DbId GetIdFromUrl(string url)
    {
        foreach (var dbIdUtil in databaseIdUtilities)
        {
            var id = dbIdUtil.GetIdFromUrl(url);
            if (id.Database != ExternalDatabase.None)
                return id;
        }
        return default;
    }

    public ExternalDatabase GetDatabaseFromPluginId(Guid libraryId)
    {
        foreach (var dbIdUtil in databaseIdUtilities)
            if (dbIdUtil.LibraryIds.Contains(libraryId))
                return dbIdUtil.Database;

        return ExternalDatabase.None;
    }

    public IEnumerable<DbId> GetIdsFromGame(Game game)
    {
        var output = new List<DbId>();
        var libraryDb = GetDatabaseFromPluginId(game.PluginId);
        if (libraryDb != ExternalDatabase.None)
            output.Add(new DbId(libraryDb, game.GameId));

        var linkIds = game.Links?.Select(l => GetIdFromUrl(l?.Url)).Where(id => id.Database != ExternalDatabase.None);
        if (linkIds != null)
            output.AddRange(linkIds);

        return output;
    }
}
