using Playnite.SDK.Models;
using System;
using System.Collections.Generic;

namespace IgnMetadata.HowLongToBeat.Models;

public enum GameType
{
    Game,
    Multi,
    Compil
}

public class HltbData : ObservableObject
{
    public GameType GameType { get; set; } = GameType.Game;

    public long MainStoryClassic { get; set; } = 0;
    public long MainStoryMedian { get; set; } = 0;
    public long MainStoryAverage { get; set; } = 0;
    public long MainStoryRushed { get; set; } = 0;
    public long MainStoryLeisure { get; set; } = 0;

    public long MainExtraClassic { get; set; } = 0;
    public long MainExtraMedian { get; set; } = 0;
    public long MainExtraAverage { get; set; } = 0;
    public long MainExtraRushed { get; set; } = 0;
    public long MainExtraLeisure { get; set; } = 0;

    public long CompletionistClassic { get; set; } = 0;
    public long CompletionistMedian { get; set; } = 0;
    public long CompletionistAverage { get; set; } = 0;
    public long CompletionistRushed { get; set; } = 0;
    public long CompletionistLeisure { get; set; } = 0;

    public long SoloClassic { get; set; } = 0;
    public long SoloMedian { get; set; } = 0;
    public long SoloAverage { get; set; } = 0;
    public long SoloRushed { get; set; } = 0;
    public long SoloLeisure { get; set; } = 0;

    public long CoOpClassic { get; set; } = 0;
    public long CoOpMedian { get; set; } = 0;
    public long CoOpAverage { get; set; } = 0;
    public long CoOpRushed { get; set; } = 0;
    public long CoOpLeisure { get; set; } = 0;

    public long VsClassic { get; set; } = 0;
    public long VsMedian { get; set; } = 0;
    public long VsAverage { get; set; } = 0;
    public long VsRushed { get; set; } = 0;
    public long VsLeisure { get; set; } = 0;
}

public class HltbDataUser : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string UrlImg { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public GameType GameType { get; set; } = GameType.Game;

    public HltbData GameHltbData { get; set; }

    public bool IsVndb { get; set; }
}

public class GameHowLongToBeat : PluginDataBaseGame<HltbDataUser>
{
    public string UserGameId { get; set; }
}

public abstract class PluginDataBaseGame<T> : PluginDataBaseGameBase
{
    /// <summary>
    /// Gets or sets the list of items associated with the game.
    /// Setting this property triggers the OnItemsChanged event and refreshes cached values.
    /// </summary>
    public List<T> Items { get; set; } = [];
}

/// <summary>
/// Base class for plugin-related game data, offering access to Playnite game information and common metadata.
/// </summary>
public class PluginDataBaseGameBase : DatabaseObject
{
    /// <summary>
    /// Gets or sets the date and time when the data was last refreshed.
    /// </summary>
    public DateTime DateLastRefresh { get; set; } = default;

    /// <summary>
    /// Indicates whether the referenced Playnite game exists in the database.
    /// </summary>
    public bool GameExist => true;
}
