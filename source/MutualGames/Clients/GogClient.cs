using AngleSharp.Parser.Html;
using MutualGames.Models.Export;
using MutualGames.Models.Settings;
using MutualGames.Services.Gog.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MutualGames.Clients;

public class GogClient(IWebViewWrapper webView) : IFriendsGamesClient
{
    private readonly HtmlParser htmlParser = new();
    private readonly ILogger logger = LogManager.GetLogger();
    private GogAccountInfo accountInfo = null;

    public string Name => "GOG";

    public FriendSource Source => FriendSource.GOG;

    public Guid PluginId { get; } = Guid.Parse("AEBE8B7C-6DC3-4A66-AF31-E7375C6B5E9E");

    public IEnumerable<string> CookieDomains => [".gog.com", "www.gog.com"];

    public string LoginUrl => "https://www.gog.com/##openlogin";

    public IEnumerable<ExternalGameData> GetFriendGames(FriendAccountInfo friend, CancellationToken cancellationToken)
    {
        var user = GetLoggedInUserAsync().Result;
        int page = 0, totalPages = 1;
        do
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            page++;
            var response = GetFriendGames(user, friend, page);
            foreach (var item in response.Embedded.Items)
            {
                yield return new ExternalGameData
                {
                    Id = item.Game.Id,
                    Name = item.Game.Title,
                    PluginId = PluginId,
                };
            }
        } while (page < totalPages);
    }

    private GogGetFriendGamesResponse GetFriendGames(GogAccountInfo account, FriendAccountInfo friend, int page)
    {
        var url = $"https://www.gog.com/u/{friend.Name}/games/stats/{account.Username}?sort=recent_playtime&order=desc&page={page}&sort_user={account.UserId}";
        var response = webView.DownloadPageTextAsync(url).Result;
        return JsonConvert.DeserializeObject<GogGetFriendGamesResponse>(response.Content);
    }

    public IEnumerable<FriendAccountInfo> GetFriends(CancellationToken cancellationToken)
    {
        var json = GetFriendsJson(cancellationToken);

        var friends = JsonConvert.DeserializeObject<GogFriendContainer[]>(json);
        foreach (var f in friends)
        {
            if (f.User?.Id == null || f.User?.Username == null)
                continue;

            yield return new FriendAccountInfo
            {
                Id = f.User.Id,
                Name = f.User.Username,
                Source = this.Source,
            };
        }
    }

    private string GetFriendsJson(CancellationToken cancellationToken)
    {
        var acctInfo = GetLoggedInUserAsync().Result;

        var response = webView.DownloadPageSource($"https://www.gog.com/u/{acctInfo.Username}/friends");
        var lines = response.Content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var l = line.Trim().TrimEnd(';');
            var trimmed = l.TrimStart("window.profilesData.profileUserFriends = ");
            if (l != trimmed)
                return trimmed;
        }
        throw new NotAuthenticatedException();
    }

    private async Task<GogAccountInfo> GetLoggedInUserAsync()
    {
        if (accountInfo != null)
            return accountInfo;

        var response = await webView.DownloadPageTextAsync("https://menu.gog.com/v1/account/basic");
        if (string.IsNullOrWhiteSpace(response.Content))
            throw new NotAuthenticatedException();

        accountInfo = JsonConvert.DeserializeObject<GogAccountInfo>(response.Content);
        if (!accountInfo.IsLoggedIn)
        {
            accountInfo = null;
            throw new NotAuthenticatedException();
        }

        return accountInfo;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var userInfo = await GetLoggedInUserAsync();
            logger.Info($"Authenticated: {userInfo?.IsLoggedIn}");
            return userInfo?.IsLoggedIn ?? false;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> IsLoginSuccessAsync(IWebView loginWebView) => IsAuthenticatedAsync();
}
