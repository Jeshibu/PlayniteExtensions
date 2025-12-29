using MutualGames.Services.Steam.Base;
using MutualGames.Services.Steam.Models;
using Playnite.SDK.Models;
using System.Collections.Generic;

namespace MutualGames.Services.Steam;

public class PlayerService : SteamApiServiceBase
{
    public IEnumerable<OwnedGame> GetOwnedGamesWeb(ulong steamUserId, string accessToken) =>
        PlayerServiceGetOwnedGames(steamUserId, "access_token", accessToken);

    public IEnumerable<OwnedGame> GetOwnedGamesApiKey(ulong steamUserId, string apiKey) =>
        PlayerServiceGetOwnedGames(steamUserId, "key", apiKey);

    private IEnumerable<OwnedGame> PlayerServiceGetOwnedGames(ulong userId, string keyType, string key)
    {
        // For some reason Steam Web API likes to return 429 even if you
        // don't make a request in several hours, so just retry couple times.
        var retrySettings = new SteamApiRetrySettings(5, 5, 429);
        var parameters = new Dictionary<string, string>
        {
            { "format", "json" },
            { keyType, key },
            { "steamid", userId.ToString() },
            { "include_appinfo", "true" },
            { "include_played_free_games", "true" },
            { "include_free_sub", "true" },
            { "language", "english" },
        };
        var response = Get<SteamApiResponseRoot<GetOwnedGamesResponse>>("https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/", parameters, retrySettings);
        return response.response.games;
    }

    private static GameMetadata ToGame(OwnedGame game, bool includePlaytime)
    {
        var output = new GameMetadata
        {
            GameId = game.appid.ToString(),
            Name = game.name.Trim(),
            Platforms = [new MetadataSpecProperty("pc_windows")],
            Source = new MetadataNameProperty("Steam"),
        };

        if (includePlaytime)
        {
            output.Playtime = game.playtime_forever * 60;
            output.LastActivity = GetLastPlayedDateTime(game.rtime_last_played);
        }

        return output;
    }
}
