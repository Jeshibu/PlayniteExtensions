using Playnite.SDK;
using PlayniteExtensions.Common;
using SqlNado;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LaunchBoxMetadata;

public class LaunchBoxDatabase
{
    public LaunchBoxDatabase(string userDataDirectory)
    {
        if (string.IsNullOrWhiteSpace(userDataDirectory))
            throw new ArgumentException($"'{nameof(userDataDirectory)}' cannot be null or whitespace.", nameof(userDataDirectory));

        UserDataDirectory = userDataDirectory;
    }

    public static string GetFilePath(string userDataFolder) => Path.Combine(userDataFolder, "LBGDB.sqlite");

    private string DatabasePath => GetFilePath(UserDataDirectory);

    private string UserDataDirectory { get; }

    private SQLiteDatabase GetConnection(SQLiteOpenOptions openOptions) => new(DatabasePath, openOptions);

    public void CreateDatabase(LaunchBoxXmlParser xmlSource, GlobalProgressActionArgs args = null)
    {
        if (args != null)
        {
            args.IsIndeterminate = false;
            args.ProgressMaxValue = 8;
        }

        void AdvanceProgress()
        {
            if (args != null)
                args.CurrentProgressValue++;
        }

        DeleteDatabase();

        var data = xmlSource.GetData();
        AdvanceProgress();

        AddAliasesToGames(data.Games, data.GameAlternateNames);
        AdvanceProgress();

        using var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_CREATE | SQLiteOpenOptions.SQLITE_OPEN_READWRITE);
        db.BeginTransaction();
        db.Save(data.Games);
        AdvanceProgress();

        var gameNames = data.Games.Select(g => new LaunchBoxGameName { DatabaseID = g.DatabaseID, Name = g.Name }).ToList();
        gameNames.AddRange(data.GameAlternateNames);
        var gameNameDeduplicatedDictionary = gameNames.ToDictionarySafe(n => $"{n.DatabaseID}|{n.Name}", StringComparer.InvariantCultureIgnoreCase);

        db.Save(gameNameDeduplicatedDictionary.Values);
        AdvanceProgress();

        db.Save(data.GameImages);
        AdvanceProgress();

        db.Save(data.GameImages.GroupBy(gi => gi.Type).Select(x => new ImageType { Name = x.Key, Count = x.Count() }));
        AdvanceProgress();

        db.Save(data.GameImages.GroupBy(gi => gi.Region).Select(x => new ImageRegion { Name = x.Key, Count = x.Count() }));
        AdvanceProgress();

        var genres = db.LoadAll<LaunchBoxGame>()
            .SelectMany(g => g.Genres.SplitLaunchBox())
            .GroupBy(g => g)
            .Select(gr => new Genre { Name = gr.Key, Count = gr.Count() }).ToList();

        db.Save(genres);
        AdvanceProgress();

        genres = db.LoadAll<Genre>().ToList(); // populate generated database IDs

        var genreIds = genres.ToDictionary(g => g.Name, g => g.Id);
        var gameGenres = db.LoadAll<LaunchBoxGame>()
            .SelectMany(g => g.Genres.SplitLaunchBox().Select(x => new GameGenre { GameId = g.DatabaseID, GenreId = genreIds[x] }))
            .ToList();

        db.Save(gameGenres);
        AdvanceProgress();

        db.Commit();
    }

    public void DeleteDatabase()
    {
        if (File.Exists(DatabasePath))
            File.Delete(DatabasePath);
    }

    private static void AddAliasesToGames(ICollection<LaunchBoxGame> games, ICollection<LaunchBoxGameName> aliases)
    {
        var gameIdDictionary = games.ToDictionary(g => g.DatabaseID);
        foreach (var a in aliases)
        {
            if (!gameIdDictionary.TryGetValue(a.DatabaseID, out var game))
                continue;

            if (string.IsNullOrEmpty(game.Aliases))
                game.Aliases = a.Name;
            else
                game.Aliases += LaunchBoxHelper.AliasSeparator + a.Name;
        }
    }

    public IEnumerable<LaunchBoxGameSearchResult> SearchGames(string search, int limit = 1000)
    {
        if (string.IsNullOrWhiteSpace(search))
            return [];

        var matchStr = GetMatchStringFromSearchString(search);

        using var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_READONLY);
        return db.Load<LaunchBoxGameSearchResult>("""
                                                   select gn.Name MatchedName, g.*
                                                   from GameNames gn
                                                   join Games g on gn.DatabaseID=g.DatabaseID
                                                   where gn.Name match ?
                                                   order by rank
                                                   limit ?
                                                   """, matchStr, limit).ToList();
    }

    public IEnumerable<Genre> GetGenres()
    {
        using var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_READONLY);

        return db.LoadAll<Genre>().ToList();
    }

    public IEnumerable<LaunchBoxGame> GetGamesForGenre(long genreId)
    {
        using var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_READONLY);

        return db.Load<LaunchBoxGame>("""
                                       SELECT g.*
                                       FROM Games g
                                       JOIN GameGenres gg on g.DatabaseID=gg.GameId
                                       WHERE gg.GenreID = ?
                                       """, genreId).ToList();
    }


    private IEnumerable<ItemCount> GetItemCounts<T>() where T : ItemCount
    {
        using var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_READONLY);
        return db.LoadAll<T>().ToList();
    }

    public IEnumerable<string> GetGameImageTypes() => GetItemCounts<ImageType>().Select(x => x.Name);
    public IEnumerable<string> GetRegions() => GetItemCounts<ImageRegion>().Select(x => x.Name);

    private static string GetMatchStringFromSearchString(string searchString)
    {
        var segments = searchString.Split(' ');
        var matchStr = new StringBuilder();
        foreach (var seg in segments)
        {
            var preppedSeg = seg.Trim(':', '-').Replace("\"", "\"\"");
            if (preppedSeg.Length == 0)
                continue;

            matchStr.Append($"\"{preppedSeg}\" ");
        }

        matchStr.Append("*");
        return matchStr.ToString();
    }
}
