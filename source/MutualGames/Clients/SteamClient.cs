using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public IEnumerable<GameDetails> GetFriendGames(FriendInfo friend)
        {
            const string urlStart = "https://store.steampowered.com/app/";

            var doc = GetHtml($"https://steamcommunity.com/profiles/{friend.Id}/games/?tab=all");
            var gameElements = doc.QuerySelectorAll($"a[href^='{urlStart}']");
            foreach (var e in gameElements)
            {
                var textContent = e.TextContent.HtmlDecode();
                if (string.IsNullOrWhiteSpace(textContent))
                    continue;

                var url = e.GetAttribute("href");

                var game = new GameDetails { Url = url, Id = url.Substring(urlStart.Length) };
                game.Names.Add(textContent);
                yield return game;
            }
        }

        public IEnumerable<FriendInfo> GetFriends()
        {
            var doc = GetHtml("https://steamcommunity.com/my/friends");
            var friendElements = doc.QuerySelectorAll(".persona[data-steamid]");
            foreach (var friendElement in friendElements)
            {
                var friendNameHtml = friendElement.QuerySelector(".friend_block_content").InnerHtml;
                var name = friendNameHtml.Split('<').First().HtmlDecode();
                yield return new FriendInfo
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
    }
}
