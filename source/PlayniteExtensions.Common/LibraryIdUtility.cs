using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
    // if you add an 8 value here, update the bit shift in DbId.GetHashCode() to 4 instead of 3
}

public struct DbId
{
    public readonly ExternalDatabase Database;
    public readonly string Id;

    public DbId(ExternalDatabase database, string id)
    {
        Database = database;
        Id = id?.ToLowerInvariant();
    }

    public override bool Equals(object obj)
    {
        if (!(obj is DbId otherId))
            return false;

        return Database == otherId.Database && Id == otherId.Id;
    }

    public override int GetHashCode()
    {
        return (int)Database ^ Id.GetHashCode() << 3;
    }

    public static DbId NoDb(string id) => new DbId(ExternalDatabase.None, id);
    public static DbId Steam(string id) => new DbId(ExternalDatabase.Steam, id);
    public static DbId GOG(string id) => new DbId(ExternalDatabase.GOG, id);
    public static DbId PCGW(string id) => new DbId(ExternalDatabase.PCGamingWiki, id);
    public static DbId Moby(string id) => new DbId(ExternalDatabase.MobyGames, id);
    public static DbId GiantBomb(string id) => new DbId(ExternalDatabase.GiantBomb, id);
    public static DbId TvTropes(string id) => new DbId(ExternalDatabase.TvTropes, id);
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
        var output = new List<(ExternalDatabase, string)>();

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
    public readonly Regex SteamUrlRegex = new Regex(@"^(steam://openurl/)?https?://(store\.steampowered\.com|steamcommunity\.com|steamdb\.info)/app/(?<id>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

    public override ExternalDatabase Database { get; } = ExternalDatabase.Steam;

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
    private readonly Regex GOGUrlRegex = new Regex(@"^https://www\.gogdb\.org/product/(?<id>[0-9]+)");

    public override ExternalDatabase Database { get; } = ExternalDatabase.GOG;

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
    private Regex UrlIdRegex = new Regex(@"\bmobygames\.com/game/(?<id>[0-9]+)(/|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public override ExternalDatabase Database { get; } = ExternalDatabase.MobyGames;

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

public class AggregateExternalDatabaseUtility : IExternalDatabaseIdUtility
{
    private List<ISingleExternalDatabaseIdUtility> databaseIdUtilities = [];

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