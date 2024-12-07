using Playnite.SDK;
using PlayniteExtensions.Common;
using SqlNado;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace LaunchBoxMetadata
{
    public class LaunchBoxDatabase
    {
        public LaunchBoxDatabase(string userDataDirectory)
        {
            if (string.IsNullOrWhiteSpace(userDataDirectory))
            {
                throw new ArgumentException($"'{nameof(userDataDirectory)}' cannot be null or whitespace.", nameof(userDataDirectory));
            }

            UserDataDirectory = userDataDirectory;
        }

        public static string GetFilePath(string userDataFolder)
        {
            return Path.Combine(userDataFolder, "LBGDB.sqlite");
        }

        private string DatabasePath => GetFilePath(UserDataDirectory);

        public string UserDataDirectory { get; }

        private SQLiteDatabase GetConnection(SQLiteOpenOptions? openOptions = null)
        {
            SQLiteDatabase db;
            if (openOptions.HasValue)
                db = new SQLiteDatabase(DatabasePath, openOptions.Value);
            else
                db = new SQLiteDatabase(DatabasePath);

            //db.EnableLoadExtension(true);
            //db.LoadExtension("System.Data.SQLite.dll", "sqlite3_fts5_init");

            return db;
        }

        public void CreateDatabase(LaunchBoxXmlParser xmlSource, GlobalProgressActionArgs args = null)
        {
            if (args != null)
            {
                args.IsIndeterminate = false;
                args.ProgressMaxValue = 4;
            }

            if (File.Exists(DatabasePath))
                File.Delete(DatabasePath);

            var data = xmlSource.GetData();
            if (args != null) args.CurrentProgressValue++;

            using (var db = GetConnection())
            {
                db.BeginTransaction();
                db.Save(data.Games);
                if (args != null) args.CurrentProgressValue++;

                var gameNames = data.Games.Select(g => new LaunchBoxGameName { DatabaseID = g.DatabaseID, Name = g.Name }).ToList();
                gameNames.AddRange(data.GameAlternateNames);
                var gameNameDeduplicatedDictionary = gameNames.ToDictionarySafe(n => $"{n.DatabaseID}|{n.Name}", StringComparer.InvariantCultureIgnoreCase);

                db.Save(gameNameDeduplicatedDictionary.Values);
                if (args != null) args.CurrentProgressValue++;

                db.Save(data.GameImages);
                if (args != null) args.CurrentProgressValue++;

                db.Commit();
            }
        }

        public IEnumerable<LaunchboxGameSearchResult> SearchGames(string search, int? limit = null)
        {
            if (string.IsNullOrWhiteSpace(search))
                return new LaunchboxGameSearchResult[0];

            var matchStr = GetMatchStringFromSearchString(search);

            var query = @"
select gn.Name MatchedName, g.*
from GameNames gn
join Games g on gn.DatabaseID=g.DatabaseID
where gn.Name match ?
order by rank";
            if (limit.HasValue)
                query += $@"
limit {limit.Value}";


            using (var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_READONLY))
            {
                return db.Load<LaunchboxGameSearchResult>(query, matchStr).ToList();
            }
        }

        public IEnumerable<string> GetGameImageTypes()
        {
            var query = @"
select distinct Type
from GameImages
order by Type asc";
            using (var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_READONLY))
            {
                return db.Load<LaunchBoxGameImage>(query).Select(i => i.Type).ToList();
            }
        }
        public IEnumerable<string> GetRegions()
        {
            var query = @"
select Region
from GameImages
group by Region
order by count(1) desc";
            using (var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_READONLY))
            {
                return db.Load<LaunchBoxGameImage>(query).Select(i => i.Region).ToList();
            }
        }

        public static string GetMatchStringFromSearchString(string searchString)
        {
            var segments = searchString.Split(' ');
            var matchStr = new StringBuilder();
            foreach (var seg in segments)
            {
                var preppedSeg = seg.Trim(':', '-').Replace("\"", "\"\"");
                if (preppedSeg.Length == 0)
                    continue;
                preppedSeg = "\"" + preppedSeg + "\" ";
                matchStr.Append(preppedSeg);
            }
            matchStr.Append("*");
            return matchStr.ToString();
        }
    }
}
