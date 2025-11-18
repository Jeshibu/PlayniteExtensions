using GamesSizeCalculator.Common.Steam;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System.Linq;

namespace GamesSizeCalculator.Steam;

public class SteamAppIdUtility : ISteamAppIdUtility
{
    private static readonly SteamIdUtility SteamIdUtility = new();

    public string GetSteamGameId(Game game)
    {
        return SteamIdUtility.GetIdsFromGame(game).FirstOrDefault().Id
               ?? SteamWeb.GetSteamIdFromSearch(game.Name);
    }
}
