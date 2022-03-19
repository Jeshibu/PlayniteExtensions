using Playnite.SDK.Models;

namespace SteamTagsImporter
{
    public interface ISteamAppIdUtility
    {
        string GetSteamGameId(Game game);
    }
}