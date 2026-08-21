using Playnite.SDK;
using PlayniteExtensions.Common;
using Rawg.Common;
using SqlNado;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RawgMetadata.Database;

public class RawgDatabase
{
    private readonly ILogger logger = LogManager.GetLogger();

    public RawgDatabase(string userDataDirectory)
    {
        if (string.IsNullOrWhiteSpace(userDataDirectory))
            throw new ArgumentException($"'{nameof(userDataDirectory)}' cannot be null or whitespace.", nameof(userDataDirectory));

        UserDataDirectory = userDataDirectory;
    }

    private string DatabasePath => Path.Combine(UserDataDirectory, "Rawg.sqlite");

    private string UserDataDirectory { get; }

    private SQLiteDatabase GetConnection(SQLiteOpenOptions openOptions) => new(DatabasePath, openOptions);

    public bool FileExists => File.Exists(DatabasePath);

    public DateTime? FileLastModified
    {
        get
        {
            var file = new FileInfo(DatabasePath);
            return file.Exists ? file.LastWriteTime : null;
        }
    }

    public void CreateDatabase(IRawgApiClient apiClient, GlobalProgressActionArgs args = null)
    {
        try
        {
            var tags = apiClient.GetTags(args);

            if (args?.CancelToken.IsCancellationRequested == true)
                return;

            args?.IsIndeterminate = false;
            args?.CurrentProgressValue = 0;
            args?.ProgressMaxValue = 3;

            args?.Text = "Deduplicating tags…";
            var deduplicatedTags = tags.Deduplicate(t => t.Id, logger);
            args?.CurrentProgressValue += 1;

            args?.Text = "Creating local database…";

            DeleteDatabase();

            args?.CurrentProgressValue += 1;

            using var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_CREATE | SQLiteOpenOptions.SQLITE_OPEN_READWRITE);

            db.Save(deduplicatedTags);

            args?.CurrentProgressValue += 1;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error creating database");
            throw;
        }
    }

    public void DeleteDatabase()
    {
        if (File.Exists(DatabasePath))
            File.Delete(DatabasePath);
    }

    public IEnumerable<RawgTag> GetAllTags()
    {
        using var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_READONLY);
        return db.LoadAll<RawgTag>().ToList();
    }

    public IEnumerable<RawgTag> SearchTags(string search, int limit = 1000)
    {
        if (string.IsNullOrWhiteSpace(search))
            return [];

        string matchStr = GetMatchStringFromSearchString(search);

        using var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_READONLY);
        return db.Load<RawgTag>($"""
                                 select t.*
                                 from {TableNames.Tags} t
                                 where t.{nameof(RawgTag.Name)} match ?
                                 order by rank
                                 limit ?
                                 """, matchStr, limit).ToList();
    }

    public int GetTagCount()
    {
        using var db = GetConnection(SQLiteOpenOptions.SQLITE_OPEN_READONLY);
        return db.TableExists<RawgTag>() ? db.Count<RawgTag>() : 0;
    }

    private static string GetMatchStringFromSearchString(string searchString)
    {
        string[] segments = searchString.Split(' ');
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
