using GamesSizeCalculator.Common.Steam;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System.Linq;

namespace GamesSizeCalculator.Steam;

public class SteamAppIdUtility : ISteamAppIdUtility
{
    private static SteamIdUtility steamIdUtility = new SteamIdUtility();

    public string GetSteamGameId(Game game)
    {
        return steamIdUtility.GetIdsFromGame(game).FirstOrDefault().Id
               ?? SteamWeb.GetSteamIdFromSearch(game.Name);
    }
}
