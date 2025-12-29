using MutualGames.Models.Export;
using MutualGames.Models.Settings;
using MutualGames.Services.Steam;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MutualGames.Clients;

public class SteamClient(IPlayniteAPI playniteApi) : IFriendsGamesClient
{
    private readonly ILogger _logger = LogManager.GetLogger();
    private readonly PlayerService _playerService = new();
    private readonly SteamUserOAuthService _oAuthService = new();
    private readonly SteamStoreService _storeService = new(playniteApi);
    private string _accessToken;
    private DateTime _accessTokenExpires = DateTime.MinValue;

    private string GetAccessToken()
    {
        if (_accessTokenExpires < DateTime.Now)
        {
            _accessToken = _storeService.GetAccessTokenAsync().Result.AccessToken;
            _accessTokenExpires = DateTime.Now.AddHours(2);
        }

        return _accessToken;
    }

    public string Name => "Steam";
    public FriendSource Source => FriendSource.Steam;
    public Guid PluginId { get; } = Guid.Parse("CB91DFC9-B977-43BF-8E70-55F46E410FAB");

    public IEnumerable<string> CookieDomains =>
    [
        "steamcommunity.com",
        ".steamcommunity.com",
        "steampowered.com",
        "store.steampowered.com",
        "help.steampowered.com",
        "login.steampowered.com",
    ];

    public string LoginUrl => "https://store.steampowered.com/login/?redir=explore%2F&redir_ssl=1";

    public IEnumerable<ExternalGameData> GetFriendGames(FriendAccountInfo friend, CancellationToken cancellationToken)
    {
        var token = GetAccessToken();
        var games = _playerService.GetOwnedGamesWeb(ulong.Parse(friend.Id), token);
        return games.Select(g => new ExternalGameData { Id = g.appid.ToString(), Name = g.name, PluginId = PluginId }).ToList();
    }

    public IEnumerable<FriendAccountInfo> GetFriends(CancellationToken cancellationToken)
    {
        var token = GetAccessToken();
        var friendships = _oAuthService.GetFriendList(token);
        var friendIds = friendships.Where(f => f.relationship == "friend").Select(f => f.steamid).ToList();
        var friends = _oAuthService.GetUserSummaries(token, friendIds);
        return friends.Select(f => new FriendAccountInfo { Id = f.steamid, Name = f.personaname, Source = FriendSource.Steam }).ToList();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await _storeService.GetAccessTokenAsync();
            _logger.Info("Authenticated!");
            return true;
        }
        catch (NotAuthenticatedException)
        {
            _logger.Info("Not authenticated");
            return false;
        }
    }

    public async Task<bool> IsLoginSuccessAsync(IWebView loginWebView)
    {
        try
        {
            var token = await _storeService.GetSteamUserTokenFromWebViewAsync(loginWebView);
            return token != null;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Not authenticated");
            return false;
        }
    }
}
