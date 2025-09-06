using SqlNado;
using System;
using PlayniteExtensions.Metadata.Common;

namespace LaunchBoxMetadata;

[SQLiteTable(Name = "Games")]
public class LaunchBoxGame : IDatabaseObject
{
    [SQLiteColumn(IsPrimaryKey = true)]
    public long DatabaseID { get; set; }
    public string Name { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int ReleaseYear { get; set; }
    public string Overview { get; set; }
    public int MaxPlayers { get; set; }
    public string ReleaseType { get; set; }
    public bool Cooperative { get; set; }
    public string WikipediaURL { get; set; }
    public string VideoURL { get; set; }
    public double CommunityRating { get; set; }
    public string Platform { get; set; }
    public string ESRB { get; set; }
    public int CommunityRatingCount { get; set; }
    public string Genres { get; set; }
    public string Developer { get; set; }
    public string Publisher { get; set; }
    
    /// <summary>
    /// Computed after XML parsing, not part of the original XML spec
    /// </summary>
    public string Aliases { get; set; }
}

[SQLiteTable(Name = "GameNames", Module = "fts5", ModuleArguments = nameof(DatabaseID) + "," + nameof(Name))]
public class LaunchBoxGameName : IDatabaseObject
{
    public long DatabaseID { get; set; }
    public string Name { get; set; }
}

[SQLiteTable(Name = "GameImages")]
public class LaunchBoxGameImage : IDatabaseObject
{
    public long DatabaseID { get; set; }

    public string FileName { get; set; }

    public string Type { get; set; }

    public string Region { get; set; }

    public uint CRC32 { get; set; }

    string IHasName.Name => FileName;
}

public class ItemCount: IHasName
{
    
    [SQLiteColumn(IsPrimaryKey = true, AutoIncrements = true)]
    public long Id { get; set; }
    public string Name { get; set; }
    public int Count { get; set; }
}

[SQLiteTable(Name = "ImageTypes")]
public class ImageType : ItemCount { }

[SQLiteTable(Name = "ImageRegions")]
public class ImageRegion : ItemCount { }

[SQLiteTable(Name = "Genres")]
public class Genre : ItemCount { }

[SQLiteTable(Name = "GameGenres")]
public class GameGenre
{
    public long GameId { get; set; }
    public long GenreId { get; set; }
}

public interface IDatabaseObject: IHasName
{
    long DatabaseID { get; }
}

public class LaunchboxGameSearchResult : LaunchBoxGame
{
    public string MatchedName { get; set; }
}
