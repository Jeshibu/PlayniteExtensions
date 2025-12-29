using System.Collections.Generic;

namespace MutualGames.Services.Steam.Models;

public class SteamApiResponseRoot<TResponse>
{
    public TResponse response { get; set; }
}

public class GetOwnedGamesResponse
{
    public int game_count { get; set; }
    public List<OwnedGame> games { get; set; }
}

public class OwnedGame
{
    public int appid { get; set; }
    public string name { get; set; }
    public uint playtime_forever { get; set; }
    public uint rtime_last_played { get; set; }
}

public class GetFriendsListResponse
{
    public SteamFriendship[] friends { get; set; }
}

public class SteamFriendship
{
    public string steamid { get; set; }
    public string relationship { get; set; }
    public int friend_since { get; set; }
}

public class GetUserSummariesResponse
{
    public UserSummary[] players { get; set; }
}

public class UserSummary
{
    public string steamid { get; set; }
    public int communityvisibilitystate { get; set; }
    public int profilestate { get; set; }
    public string personaname { get; set; }
    public string profileurl { get; set; }
    public string avatar { get; set; }
    public string avatarmedium { get; set; }
    public string avatarfull { get; set; }
    public string avatarhash { get; set; }
    public int lastlogoff { get; set; }
    public int personastate { get; set; }
    public string primaryclanid { get; set; }
    public int timecreated { get; set; }
    public int personastateflags { get; set; }
    public string loccountrycode { get; set; }
    public string realname { get; set; }
}
