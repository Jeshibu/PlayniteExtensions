using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using SqlNado;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void CreateDatabase(LaunchBoxXmlParser xmlSource)
        {
            if (File.Exists(DatabasePath))
                File.Delete(DatabasePath);

            var data = xmlSource.GetData();

            using (var db = new SQLiteDatabase(DatabasePath))
            {
                db.BeginTransaction();
                db.Save(data.Games);

                var gameNames = data.Games.Select(g => new LaunchBoxGameName { DatabaseID = g.DatabaseID, Name = g.Name }).ToList();
                gameNames.AddRange(data.GameAlternateNames);
                var gameNameDeduplicatedDictionary = gameNames.ToDictionarySafe(n => $"{n.DatabaseID}|{n.Name}", StringComparer.InvariantCultureIgnoreCase);

                db.Save(gameNameDeduplicatedDictionary.Values);

                db.Save(data.GameImages);
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


            using (var db = new SQLiteDatabase(DatabasePath, SQLiteOpenOptions.SQLITE_OPEN_READONLY))
            {
                return db.Load<LaunchboxGameSearchResult>(query, matchStr).ToList();
            }
        }

        public IEnumerable<LaunchBoxGameImage> GetGameImages(string id)
        {
            var query = @"
select *
from GameImages
where DatabaseID = ?";

            using (var db = new SQLiteDatabase(DatabasePath, SQLiteOpenOptions.SQLITE_OPEN_READONLY))
            {
                return db.Load<LaunchBoxGameImage>(query, id).ToList();
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
