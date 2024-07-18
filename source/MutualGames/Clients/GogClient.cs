using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MutualGames.Clients
{
    public class GogClient : IFriendsGamesClient
    {
        private readonly IWebViewWrapper offscreenWebView;
        private readonly HtmlParser htmlParser = new HtmlParser();

        public GogClient(IWebViewWrapper webView)
        {
            this.offscreenWebView = webView;
        }

        public string Name { get; } = "GOG";

        public Guid PluginId { get; } = Guid.Parse("AEBE8B7C-6DC3-4A66-AF31-E7375C6B5E9E");

        public IEnumerable<string> CookieDomains => new[] { ".gog.com", "www.gog.com" };

        public string LoginUrl => "https://www.gog.com/##openlogin";

        public IEnumerable<GameDetails> GetFriendGames(FriendInfo friend)
        {
            var user = GetLoggedInUserAsync().Result;
            int page = 0, totalPages = 1;
            do
            {
                page++;
                var response = GetFriendGames(user, friend, page);
                foreach (var item in response.Embedded.Items)
                {
                    var game = new GameDetails
                    {
                        Id = item.Game.Id,
                        Url = item.Game.Url.GetAbsoluteUrl("https://www.gog.com"),
                    };
                    game.Names.Add(item.Game.Title);
                    yield return game;
                }
            } while (page < totalPages);
        }

        private GetFriendGamesResponse GetFriendGames(AccountInfo account, FriendInfo friend, int page)
        {
            var url = $"https://www.gog.com/u/{friend.Name}/games/stats/{account.Username}?sort=recent_playtime&order=desc&page={page}&sort_user={account.UserId}";
            var response = offscreenWebView.DownloadPageSource(url);
            return JsonConvert.DeserializeObject<GetFriendGamesResponse>(response.Content);
        }

        public IEnumerable<FriendInfo> GetFriends()
        {
            var json = GetFriendsJson();

            var acctInfo = JsonConvert.DeserializeObject<AccountRoot>(json);
            foreach (var f in acctInfo.Friends)
            {
                yield return new FriendInfo
                {
                    Id = f.Id,
                    Name = f.Username,
                    Source = this.Name,
                };
            }
        }

        private string GetFriendsJson()
        {
            var response = offscreenWebView.DownloadPageSource("https://www.gog.com/account/friends");
            var lines = response.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var l = line.Trim().TrimEnd(';');
                var trimmed = l.TrimStart("var gogData = ");
                if (l != trimmed)
                    return trimmed;
            }
            throw new NotAuthenticatedException();
        }

        private async Task<AccountInfo> GetLoggedInUserAsync()
        {
            var response = await offscreenWebView.DownloadPageSourceAsync("https://menu.gog.com/v1/account/basic");
            if (string.IsNullOrWhiteSpace(response.Content))
                throw new NotAuthenticatedException();

            var accountInfo = JsonConvert.DeserializeObject<AccountInfo>(response.Content);
            if (!accountInfo.IsLoggedIn)
                throw new NotAuthenticatedException();

            return accountInfo;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var userInfo = await GetLoggedInUserAsync();
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
            public List<GameAndStats> Items { get; set; } = new List<GameAndStats>();
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


        private class AccountRoot
        {
            public List<GogFriend> Friends { get; set; } = new List<GogFriend>();
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
}
