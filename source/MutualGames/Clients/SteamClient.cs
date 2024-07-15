using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MutualGames.Clients
{
    public class SteamClient : IFriendsGamesClient
    {
        private readonly IWebViewWrapper webView;
        private readonly HtmlParser htmlParser = new HtmlParser();

        public SteamClient(IWebViewWrapper webView)
        {
            this.webView = webView;
        }

        public string Name { get; } = "Steam";
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
                    Source = this.Name,
                };
            }
        }

        private IHtmlDocument GetHtml(string url)
        {
            var response = webView.DownloadPageSource(url);
            var doc = htmlParser.Parse(response.Content);
            GateAuthentication(doc);
            return doc;
        }

        private bool IsAuthenticated(string pageSource) => IsAuthenticated(htmlParser.Parse(pageSource));
        private bool IsAuthenticated(IHtmlDocument doc) => doc.QuerySelector("#account_pulldown") != null;

        private void GateAuthentication(IHtmlDocument doc)
        {
            if (!IsAuthenticated(doc))
                throw new NotAuthenticatedException();
        }

        public bool IsAuthenticated()
        {
            try
            {
                var doc = GetHtml("https://steamcommunity.com/search/groups");
                return false;
            }
            catch (NotAuthenticatedException)
            {
                return false;
            }
        }

        public bool IsLoginSuccess(IWebView loginWebView) => IsAuthenticated(loginWebView.GetPageSource());
    }
}
