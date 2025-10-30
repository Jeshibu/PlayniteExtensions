// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
namespace EaLibrary.Models;

public class GetGamesPlayTimesRoot
{
    public RecentGamesUser me { get; set; }
}

public class RecentGamesUser
{
    public string id { get; set; }
    public RecentGames recentGames { get; set; }
    public string __typename { get; set; }
}

public class RecentGames
{
    public GamePlayTime[] items { get; set; }
    public string __typename { get; set; }
}

public class GamePlayTime
{
    public string gameSlug { get; set; }
    public string lastSessionEndDate { get; set; }
    public ulong? totalPlayTimeSeconds { get; set; }
    public string __typename { get; set; }
}
