using AngleSharp.Parser.Html;
using MutualGames.Models.Export;
using MutualGames.Models.Settings;
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
    private readonly HtmlParser htmlParser = new HtmlParser();
    private readonly ILogger logger = LogManager.GetLogger();
    private AccountInfo accountInfo = null;

    public string Name { get; } = "GOG";

    public FriendSource Source { get; } = FriendSource.GOG;

    public Guid PluginId { get; } = Guid.Parse("AEBE8B7C-6DC3-4A66-AF31-E7375C6B5E9E");

    public IEnumerable<string> CookieDomains => new[] { ".gog.com", "www.gog.com" };

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

    private GetFriendGamesResponse GetFriendGames(AccountInfo account, FriendAccountInfo friend, int page)
    {
        var url = $"https://www.gog.com/u/{friend.Name}/games/stats/{account.Username}?sort=recent_playtime&order=desc&page={page}&sort_user={account.UserId}";
        var response = webView.DownloadPageTextAsync(url).Result;
        return JsonConvert.DeserializeObject<GetFriendGamesResponse>(response.Content);
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
        var lines = response.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var l = line.Trim().TrimEnd(';');
            var trimmed = l.TrimStart("window.profilesData.profileUserFriends = ");
            if (l != trimmed)
                return trimmed;
        }
        throw new NotAuthenticatedException();
    }

    private async Task<AccountInfo> GetLoggedInUserAsync()
    {
        if (accountInfo != null)
            return accountInfo;

        var response = await webView.DownloadPageTextAsync("https://menu.gog.com/v1/account/basic");
        if (string.IsNullOrWhiteSpace(response.Content))
            throw new NotAuthenticatedException();

        accountInfo = JsonConvert.DeserializeObject<AccountInfo>(response.Content);
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

    #region models
    private class GetFriendGamesResponse
    {
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Pages { get; set; }
        public int Total { get; set; }

        [JsonProperty("_embedded")]
        public EmbeddedGames Embedded { get; set; }
    }

    private class EmbeddedGames
    {
        public List<GameAndStats> Items { get; set; } = [];
    }

    private class GameAndStats
    {
        public GogGame Game { get; set; }
    }

    private class GogGame
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public bool AchievementSupport { get; set; }
        public string Image { get; set; }
    }

    private class GogFriendContainer
    {
        public GogFriend User { get; set; }
    }

    private class GogFriend
    {
        public string Id { get; set; }
        public string Username { get; set; }
    }

    private class AccountInfo
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public bool IsLoggedIn { get; set; }
    }
    #endregion models
}
