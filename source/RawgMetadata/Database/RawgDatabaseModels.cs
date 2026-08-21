using Newtonsoft.Json;
using PlayniteExtensions.Metadata.Common;
using Rawg.Common;
using SqlNado;

namespace RawgMetadata.Database;

public static class TableNames
{
    public const string Tags = "RawgTags";
}

[SQLiteTable(Name = TableNames.Tags, Module = "fts5", ModuleArguments = $"{nameof(Id)},{nameof(Slug)},{nameof(Name)},{nameof(Language)},{nameof(GamesCount)}")]
public class RawgTag : RawgLocalizedObject, IHasName
{
    [JsonProperty("games_count")]
    public int GamesCount { get; set; }
}
