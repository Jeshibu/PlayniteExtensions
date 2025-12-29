namespace MutualGames.Services.Steam.Models;

public struct SteamUserToken(ulong userId, string accessToken)
{
    public readonly ulong UserId = userId;
    public readonly string AccessToken = accessToken;
}

public class StoreUserConfig
{
    public string webapi_token { get; set; }
}

public class UserInfo
{
    public bool logged_in { get; set; }
    public string steamid { get; set; }
    public string account_name { get; set; }
}
