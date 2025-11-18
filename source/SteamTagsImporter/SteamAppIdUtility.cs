using GamesSizeCalculator.Common.Steam;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace SteamTagsImporter;

public class SteamAppIdUtility(ICachedFile steamAppList) : ISteamAppIdUtility
{
    private readonly SteamIdUtility steamIdUtility = new();

    private static readonly Regex NonLetterOrDigitCharacterRegex = new(@"[^\p{L}\p{Nd}]", RegexOptions.Compiled);

    public ICachedFile SteamAppList { get; } = steamAppList;

    private static string NormalizeTitle(string title)
    {
        return NonLetterOrDigitCharacterRegex.Replace(title, string.Empty);
    }

    public string GetSteamGameId(Game game)
    {
        var ids = steamIdUtility.GetIdsFromGame(game).ToList();
        if (ids.Any())
            return ids[0].Id;

        return SteamWeb.GetSteamIdFromSearch(game.Name);
    }
}
