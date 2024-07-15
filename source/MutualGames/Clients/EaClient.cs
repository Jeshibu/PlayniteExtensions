using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MutualGames.Clients
{
    public class EaClient : IFriendsGamesClient
    {
        private readonly IWebViewWrapper webView;
        private readonly IWebDownloader downloader;
        private readonly ILogger logger = LogManager.GetLogger();
        private AuthTokenResponse _authToken;
        private long _userId;
        private AuthTokenResponse AuthToken => _authToken ?? (_authToken = GetAccessToken());
        private long UserId => _userId != default ? _userId : (_userId = GetUserId(AuthToken));

        public string Name { get; } = "EA";

        public Guid PluginId { get; } = Guid.Parse("85DD7072-2F20-4E76-A007-41035E390724");

        public IEnumerable<string> CookieDomains => new[] { ".ea.com", "myaccount.ea.com" };

        public string LoginUrl => "https://myaccount.ea.com/cp-ui/aboutme/index";

        public EaClient(IWebViewWrapper webView, IWebDownloader downloader)
        {
            this.webView = webView;
            this.downloader = downloader;
        }

        public IEnumerable<GameDetails> GetFriendGames(FriendInfo friend)
        {
            var response = downloader.DownloadString($"https://api3.origin.com/atom/users/{UserId}/other/{friend.Id}/games", headerSetter: headers =>
            {
                headers.Set("AuthToken", AuthToken.access_token);
                headers.Set(HttpRequestHeader.Accept, "application/json");
            });
            var gamesResponse = JsonConvert.DeserializeObject<ProductInfosResponse>(response.ResponseContent);
            if (gamesResponse.productInfos == null)
                yield break;

            foreach (var product in gamesResponse.productInfos)
            {
                yield return new GameDetails
                {
                    Id = product.productId,
                    Names = new List<string> { product.displayProductName },
                };
            }
        }

        public IEnumerable<FriendInfo> GetFriends()
        {
            string url = $"https://friends.gs.ea.com/friends/2/users/{UserId}/friends?names=true";
            var response = downloader.DownloadString(url, headerSetter: headers =>
            {
                headers.Set("AuthToken", AuthToken.access_token);
                headers.Set("X-Api-Version", "2");
                headers.Set("X-Application-Key", "Origin");
            });
            var friendsResponse = JsonConvert.DeserializeObject<FriendsResponse>(response.ResponseContent);

            if (friendsResponse?.entries == null)
                return null;

            return friendsResponse?.entries?.Select(e => new FriendInfo { Id = e.userId.ToString(), Name = e.nickName, Source = this.Name });
        }

        public bool IsAuthenticated()
        {
            try
            {
                _authToken = GetValidAccessToken();
                return _authToken != null;
            }
            catch (NotAuthenticatedException)
            {
                return false;
            }
        }

        private AuthTokenResponse GetAccessToken()
        {
            try
            {
                var response = webView.DownloadPageSource("https://accounts.ea.com/connect/auth?client_id=ORIGIN_JS_SDK&response_type=token&redirect_uri=nucleus:rest&prompt=none");
                var tokenData = Serialization.FromJson<AuthTokenResponse>(response.Content);
                return tokenData;
            }
            catch (Exception e) { throw new NotAuthenticatedException("Error getting access token", e); }
        }

        private AuthTokenResponse GetValidAccessToken()
        {
            var authToken = GetAccessToken();
            if (string.IsNullOrEmpty(authToken.error))
                return authToken;

            return null;
        }

        private long GetUserId(AuthTokenResponse authToken)
        {
            try
            {
                var response = downloader.DownloadString("https://gateway.ea.com/proxy/identity/pids/me", headerSetter: headers =>
                {
                    headers.Set(HttpRequestHeader.Authorization, $"{authToken.token_type} {authToken.access_token}");
                });
                var accountInfo = JsonConvert.DeserializeObject<AccountInfoResponse>(response.ResponseContent);
                return accountInfo.pid.pidId;
            }
            catch (Exception e)
            {
                throw new NotAuthenticatedException("Error getting user ID", e);
            }
        }

        public bool IsLoginSuccess(IWebView loginWebView)
        {
            _authToken = null;
            _userId = default;
            return IsAuthenticated();
        }

        #region models

        private class AuthTokenResponse
        {
            public string error;
            public string expires_in;
            public string token_type;
            public string access_token;
        }

        private class AccountInfoResponse
        {
            public Pid pid { get; set; }
        }

        private class Pid
        {
            public long pidId { get; set; }
        }

        private class FriendsResponse
        {
            public PagingInfo pagingInfo { get; set; }
            public List<FriendEntry> entries { get; set; }
        }

        private class PagingInfo
        {
            public int totalSize { get; set; }
            public int size { get; set; }
            public int offset { get; set; }
        }

        private class FriendEntry
        {
            public string displayName { get; set; }
            public long timestamp { get; set; }
            public string friendType { get; set; }
            public DateTime? dateTime { get; set; }
            public long userId { get; set; }
            public long personaId { get; set; }
            public bool favorite { get; set; }
            public string nickName { get; set; }
            public string userType { get; set; }
        }

        private class ProductInfosResponse
        {
            public List<ProductInfo> productInfos { get; set; }
        }
        private class SoftwareList
        {
            public string softwarePlatform { get; set; }
            public string achievementSetOverride { get; set; }
        }

        private class Softwares
        {
            public List<SoftwareList> softwareList { get; set; }
        }

        private class ProductInfo
        {
            public string productId { get; set; }
            public string displayProductName { get; set; }
            public string cdnAssetRoot { get; set; }
            public string imageServer { get; set; }
            public object backgroundImage { get; set; }
            public string packArtSmall { get; set; }
            public string packArtMedium { get; set; }
            public string packArtLarge { get; set; }
            public Softwares softwares { get; set; }
            public string masterTitleId { get; set; }
            public string gameDistributionSubType { get; set; }
            public int gameEditionTypeFacetKeyRankDesc { get; set; }
        }
        #endregion models
    }
}
