using SqlNado;
using System;

namespace LaunchBoxMetadata;

[SQLiteTable(Name = "Games")]
public class LaunchBoxGame : IDatabaseObject
{
    [SQLiteColumn(IsPrimaryKey = true)]
    public string DatabaseID { get; set; }
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
}

[SQLiteTable(Name = "GameNames", Module = "fts5", ModuleArguments = nameof(DatabaseID) + "," + nameof(Name))]
public class LaunchBoxGameName : IDatabaseObject
{
    public string DatabaseID { get; set; }
    public string Name { get; set; }
}

[SQLiteTable(Name = "GameImages")]
public class LaunchBoxGameImage : IDatabaseObject
{
    public string DatabaseID { get; set; }

    public string FileName { get; set; }

    public string Type { get; set; }

    public string Region { get; set; }

    public uint CRC32 { get; set; }

    string IDatabaseObject.Name => FileName;
}

public class ItemCount
{
    public string Name { get; set; }
    public int Count { get; set; }
}

public class ImageType : ItemCount { }
public class ImageRegion : ItemCount { }

public interface IDatabaseObject
{
    string DatabaseID { get; }
    string Name { get; }
}

public class LaunchboxGameSearchResult : LaunchBoxGame
{
    public string MatchedName { get; set; }
}
