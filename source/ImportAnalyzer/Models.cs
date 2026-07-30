using Playnite.SDK.Models;
using System.Collections.Generic;

namespace ImportAnalyzer;

public class AlreadyImportedGameInfo
{
    public GameMetadata ImportedGame { get; set; }
    public Game LibraryGame { get; set; }
    public string DifferentNameInLibrary => ImportedGame.Name != LibraryGame.Name ? LibraryGame.Name : string.Empty;
}

public class LibraryImportResult
{
    public List<GameMetadata> NewlyImportGames { get; set; } = [];
    public string NewlyImportGamesHeader => $"Newly imported ({NewlyImportGames?.Count} games)";

    public List<AlreadyImportedGameInfo> AlreadyImportedGames { get; set; } = [];
    public string AlreadyImportedGamesHeader => $"Already imported before ({AlreadyImportedGames?.Count} games)";

    public List<GameMetadata> ExcludedGames { get; set; } = [];
    public string ExcludedGamesHeader => $"Excluded ({ExcludedGames?.Count} games)";

    public List<Game> MissingGames { get; set; } = [];
    public string MissingGamesHeader => $"In library but missing from import ({MissingGames?.Count} games)";
}
