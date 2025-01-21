using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PlayniteExtensions.Common
{
    public enum ExternalDatabase
    {
        None,
        Steam,
        GOG,
        PCGamingWiki,
        MobyGames,
        GiantBomb,
        TvTropes,
    }

    public interface IExternalDatabaseIdUtility
    {
        IEnumerable<ExternalDatabase> Databases { get; }
        (ExternalDatabase Database, string Id) GetIdFromUrl(string url);
        ExternalDatabase GetDatabaseFromPluginId(Guid libraryId);
        IEnumerable<(ExternalDatabase Database, string Id)> GetIdsFromGame(Game game);
    }

    public interface ISingleExternalDatabaseIdUtility : IExternalDatabaseIdUtility
    {
        ExternalDatabase Database { get; }
        IEnumerable<Guid> LibraryIds { get; }
    }

    public abstract class SingleExternalDatabaseIdUtility : ISingleExternalDatabaseIdUtility
    {
        public IEnumerable<ExternalDatabase> Databases => new[] { Database };

        public abstract ExternalDatabase Database { get; }

        public abstract IEnumerable<Guid> LibraryIds { get; }

        public ExternalDatabase GetDatabaseFromPluginId(Guid pluginId)
        {
            if (LibraryIds.Contains(pluginId))
                return Database;

            return ExternalDatabase.None;
        }

        public abstract (ExternalDatabase Database, string Id) GetIdFromUrl(string url);

        public IEnumerable<(ExternalDatabase Database, string Id)> GetIdsFromGame(Game game)
        {
            var output = new List<(ExternalDatabase, string)>();

            if (LibraryIds.Contains(game.PluginId))
                yield return (Database, game.GameId);

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

        public override IEnumerable<Guid> LibraryIds { get; } = new[] { Guid.Parse("CB91DFC9-B977-43BF-8E70-55F46E410FAB") };

        public override (ExternalDatabase Database, string Id) GetIdFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return (ExternalDatabase.None, null);

            var match = SteamUrlRegex.Match(url);
            if (match.Success)
                return (ExternalDatabase.Steam, match.Groups["id"].Value);

            return (ExternalDatabase.None, null);
        }
    }

    public class GOGIdUtility : SingleExternalDatabaseIdUtility
    {
        private readonly Regex GOGUrlRegex = new Regex(@"^https://www\.gogdb\.org/product/(?<id>[0-9]+)");

        public override ExternalDatabase Database { get; } = ExternalDatabase.GOG;

        public override IEnumerable<Guid> LibraryIds => new[] {
            Guid.Parse("AEBE8B7C-6DC3-4A66-AF31-E7375C6B5E9E"), // GOG
            Guid.Parse("03689811-3F33-4DFB-A121-2EE168FB9A5C"), // GOG OSS
        };

        public override (ExternalDatabase Database, string Id) GetIdFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return (ExternalDatabase.None, null);

            var match = GOGUrlRegex.Match(url);
            if (match.Success)
                return (ExternalDatabase.GOG, match.Groups["id"].Value);

            return (ExternalDatabase.None, null);
        }
    }

    public class MobyGamesIdUtility : SingleExternalDatabaseIdUtility
    {
        private Regex UrlIdRegex = new Regex(@"\bmobygames\.com/game/(?<id>[0-9]+)(/|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public override ExternalDatabase Database { get; } = ExternalDatabase.MobyGames;

        public override IEnumerable<Guid> LibraryIds { get; } = new Guid[0];

        public override (ExternalDatabase Database, string Id) GetIdFromUrl(string url)
        {
            if (url == null) return (ExternalDatabase.None, null);

            var match = UrlIdRegex.Match(url);
            if (!match.Success) return (ExternalDatabase.None, null);
            var idString = match.Groups["id"].Value;
            return (ExternalDatabase.MobyGames, idString);
        }
    }

    public class AggregateExternalDatabaseUtility : IExternalDatabaseIdUtility
    {
        private List<ISingleExternalDatabaseIdUtility> databaseIdUtilities = new List<ISingleExternalDatabaseIdUtility>();

        public IEnumerable<ExternalDatabase> Databases => databaseIdUtilities.SelectMany(x => x.Databases).Distinct();

        public AggregateExternalDatabaseUtility(params ISingleExternalDatabaseIdUtility[] dbIdUtilities)
        {
            databaseIdUtilities.AddRange(dbIdUtilities);
        }

        public (ExternalDatabase Database, string Id) GetIdFromUrl(string url)
        {
            foreach (var dbIdUtil in databaseIdUtilities)
            {
                var id = dbIdUtil.GetIdFromUrl(url);
                if (id.Database != ExternalDatabase.None)
                    return id;
            }
            return (ExternalDatabase.None, null);
        }

        public ExternalDatabase GetDatabaseFromPluginId(Guid libraryId)
        {
            foreach (var dbIdUtil in databaseIdUtilities)
                if (dbIdUtil.LibraryIds.Contains(libraryId))
                    return dbIdUtil.Database;

            return ExternalDatabase.None;
        }

        public IEnumerable<(ExternalDatabase Database, string Id)> GetIdsFromGame(Game game)
        {
            var output = new List<(ExternalDatabase, string)>();
            var libraryDb = GetDatabaseFromPluginId(game.PluginId);
            if (libraryDb != ExternalDatabase.None)
                output.Add((libraryDb, game.GameId));

            var linkIds = game.Links?.Select(l => GetIdFromUrl(l?.Url)).Where(id => id.Database != ExternalDatabase.None);
            if (linkIds != null)
                output.AddRange(linkIds);

            return output;
        }
    }
}