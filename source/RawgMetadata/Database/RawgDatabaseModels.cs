using Newtonsoft.Json;
using PlayniteExtensions.Metadata.Common;
using Rawg.Common;
using SqlNado;

namespace RawgMetadata.Database;

public static class TableNames
{
    public const string Tags = "RawgTags";
}

[SQLiteTable(Name = TableNames.Tags, Module = "fts5", ModuleArguments = nameof(Name))]
public class RawgTag : RawgLocalizedObject, IHasName
{
    /*
    //[SQLiteColumn(IsPrimaryKey = true, AutoIncrements = false)]
    public int Id { get; set; }

    public string Slug { get; set; }

    public string Name { get; set; }

    /// <summary>
    /// https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes
    /// </summary>
    public string Language { get; set; }
    */

    //[JsonProperty("games_count")]
    public int GamesCount { get; set; }
}
