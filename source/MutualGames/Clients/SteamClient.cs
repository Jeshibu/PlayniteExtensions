using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using MutualGames.Models.Export;
using MutualGames.Models.Settings;
using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MutualGames.Clients
{
    public class SteamClient : IFriendsGamesClient
    {
        private readonly IWebViewWrapper webView;
        private readonly HtmlParser htmlParser = new HtmlParser();
        private readonly ILogger logger = LogManager.GetLogger();

        public SteamClient(IWebViewWrapper webView)
        {
            this.webView = webView;
        }

        public string Name { get; } = "Steam";
        public FriendSource Source { get; } = FriendSource.Steam;
        public Guid PluginId { get; } = Guid.Parse("CB91DFC9-B977-43BF-8E70-55F46E410FAB");

        public IEnumerable<string> CookieDomains => new[] { "steamcommunity.com" };

        public string LoginUrl => "https://steamcommunity.com/login/home/?goto=search%2Fgroups";

        public IEnumerable<ExternalGameData> GetFriendGames(FriendAccountInfo friend, CancellationToken cancellationToken)
        {
            const string urlStart = "https://store.steampowered.com/app/";
            const string jsonAttr = "data-profile-gameslist";

            var doc = GetHtml($"https://steamcommunity.com/profiles/{friend.Id}/games/?tab=all");
            var gamesListTemplate = doc.QuerySelector($"template#gameslist_config[{jsonAttr}]");
            var jsonStr = gamesListTemplate.GetAttribute(jsonAttr).HtmlDecode();
            var json = JsonConvert.DeserializeObject<ProfileDataRoot>(jsonStr);
            foreach (var g in json.rgGames)
            {
                var id = g.appid.ToString();
                var url = urlStart + id;

                yield return new ExternalGameData { Id = id, Name = g.name, PluginId = PluginId };
            }
        }

        public IEnumerable<FriendAccountInfo> GetFriends(CancellationToken cancellationToken)
        {
            var doc = GetHtml("https://steamcommunity.com/my/friends");
            var friendElements = doc.QuerySelectorAll(".persona[data-steamid]");
            foreach (var friendElement in friendElements)
            {
                var friendNameHtml = friendElement.QuerySelector(".friend_block_content").InnerHtml;
                var name = friendNameHtml.Split('<').First().HtmlDecode();
                yield return new FriendAccountInfo
                {
                    Id = friendElement.GetAttribute("data-steamid"),
                    Name = name,
                    Source = this.Source,
                };
            }
        }

        private IHtmlDocument GetHtml(string url)
        {
            var response = webView.DownloadPageSource(url);
            var doc = htmlParser.Parse(response.Content);
            GateAuthentication(doc, response.Url);
            return doc;
        }

        private async Task<IHtmlDocument> GetHtmlAsync(string url)
        {
            var response = await webView.DownloadPageSourceAsync(url);
            var doc = await htmlParser.ParseAsync(response.Content);
            GateAuthentication(doc, response.Url);
            return doc;
        }

        private bool IsAuthenticated(string pageSource, string url) => IsAuthenticated(htmlParser.Parse(pageSource), url);
        private bool IsAuthenticated(IHtmlDocument doc, string url)
        {
            bool authenticated = doc.QuerySelector("#account_pulldown") != null;
            logger.Info($"Url {url} authenticated: {authenticated}");
            return authenticated;
        }

        private void GateAuthentication(IHtmlDocument doc, string url)
        {
            if (!IsAuthenticated(doc, url))
                throw new NotAuthenticatedException();
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var doc = await GetHtmlAsync("https://steamcommunity.com/search/groups"); //gated for authentication
                logger.Info($"Authenticated!");
                return true;
            }
            catch (NotAuthenticatedException)
            {
                logger.Info("Not authenticated");
                return false;
            }
        }

        public async Task<bool> IsLoginSuccessAsync(IWebView loginWebView) => IsAuthenticated(await loginWebView.GetPageSourceAsync(), loginWebView.GetCurrentAddress());

        #region models

        private class ProfileDataRoot
        {
            public List<SteamGame> rgGames { get; set; } = new List<SteamGame>();
        }

        private class SteamGame
        {
            public long appid { get; set; }
            public string name { get; set; }
        }

        #endregion models
    }
}
