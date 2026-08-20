using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace ImportAnalyzer;

public class AlreadyImportedGameInfo
{
    public GameMetadata ImportedGame { get; set; }
    public Game LibraryGame { get; set; }
    public string DifferentNameInLibrary => ImportedGame?.Name != LibraryGame?.Name ? LibraryGame?.Name : string.Empty;
    public string ImportSource => ImportedGame?.Source?.ToString();
    public string LibrarySource => LibraryGame?.Source?.ToString();
}

public class ImpGame(GameMetadata importGame)
{
    public GameMetadata ImportedGame { get; } = importGame;
    public string ImportSource => ImportedGame?.Source?.ToString();
}

public class LibGame(Game libraryGame)
{
    public Game LibraryGame { get; } = libraryGame;
    public string LibrarySource => LibraryGame?.Source?.ToString();
}

public class LibraryImportResult
{
    public LibraryPlugin LibraryPlugin { get; set; }

    public List<ImpGame> NewlyImportGames { get; set; } = [];
    public string NewlyImportGamesHeader => $"Newly imported ({NewlyImportGames?.Count} games)";

    public List<AlreadyImportedGameInfo> AlreadyImportedGames { get; set; } = [];
    public string AlreadyImportedGamesHeader => $"Already imported before ({AlreadyImportedGames?.Count} games)";

    public List<ImpGame> ExcludedGames { get; set; } = [];
    public string ExcludedGamesHeader => $"Excluded ({ExcludedGames?.Count} games)";

    public List<LibGame> MissingGames { get; set; } = [];
    public string MissingGamesHeader => $"In library but missing from import ({MissingGames?.Count} games)";
}

public class SourceOption
{
    public string SourceName { get; set; }
    public int Count { get; set; }
    public bool Checked { get; set; }
}

public class TagMissingGamesPromptViewModel
{
    public LibraryImportResult ImportResult { get; set; }
    public List<SourceOption> SourceOptions { get; set; } = [];
}
