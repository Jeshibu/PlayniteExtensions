using MutualGames.Services.Steam.Base;
using MutualGames.Services.Steam.Models;
using System.Collections.Generic;

namespace MutualGames.Services.Steam;

public class SteamUserOAuthService : SteamApiServiceBase
{
    public SteamFriendship[] GetFriendList(string accessToken)
    {
        var response = Get<GetFriendsListResponse>("https://api.steampowered.com/ISteamUserOAuth/GetFriendList/v1/", new() { { "access_token", accessToken } });
        return response.friends;
    }

    public UserSummary[] GetUserSummaries(string accessToken, IEnumerable<string> steamIds)
    {
        var response = Get<GetUserSummariesResponse>("https://api.steampowered.com/ISteamUserOAuth/GetUserSummaries/v1/",
                                                     new()
                                                     {
                                                         { "access_token", accessToken },
                                                         { "steamids", string.Join(",", steamIds) }
                                                     });
        return response.players;
    }
}
