using Newtonsoft.Json;
using System.Collections.Generic;

namespace MutualGames.Services.Gog.Models;

internal class GogGetFriendGamesResponse
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Pages { get; set; }
    public int Total { get; set; }

    [JsonProperty("_embedded")] public GogEmbeddedGames Embedded { get; set; }
}

internal class GogEmbeddedGames
{
    public List<GogGameAndStats> Items { get; set; } = [];
}

internal class GogGameAndStats
{
    public GogGame Game { get; set; }
}

internal class GogGame
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public bool AchievementSupport { get; set; }
    public string Image { get; set; }
}

internal class GogFriendContainer
{
    public GogFriend User { get; set; }
}

internal class GogFriend
{
    public string Id { get; set; }
    public string Username { get; set; }
}

internal class GogAccountInfo
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public bool IsLoggedIn { get; set; }
}
