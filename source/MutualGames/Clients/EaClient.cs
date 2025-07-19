using MutualGames.Models.Export;
using MutualGames.Models.Settings;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MutualGames.Clients;

public class EaClient(IWebViewWrapper webView, IWebDownloader downloader) : IFriendsGamesClient
{
    private readonly ILogger logger = LogManager.GetLogger();
    private AuthTokenResponse _authToken;
    private long _userId;
    private AuthTokenResponse AuthToken => _authToken ??= GetAccessTokenAsync().Result;
    private long UserId => _userId != default ? _userId : (_userId = GetUserId(AuthToken));

    public string Name { get; } = "EA";

    public FriendSource Source { get; } = FriendSource.EA;

    public Guid PluginId { get; } = Guid.Parse("85DD7072-2F20-4E76-A007-41035E390724");

    public IEnumerable<string> CookieDomains => new[] { ".ea.com", "myaccount.ea.com" };

    public string LoginUrl => "https://myaccount.ea.com/cp-ui/aboutme/index";

    public IEnumerable<ExternalGameData> GetFriendGames(FriendAccountInfo friend, CancellationToken cancellationToken)
    {
        var userId = UserId;
        var response = downloader.DownloadString($"https://api3.origin.com/atom/users/{userId}/other/{friend.Id}/games",
            cancellationToken: cancellationToken,
            headerSetter: headers =>
        {
            headers.Add("AuthToken", AuthToken.access_token);
        });
        var gamesResponse = JsonConvert.DeserializeObject<ProductInfosResponse>(response.ResponseContent);
        if (gamesResponse.productInfos == null)
            yield break;

        foreach (var product in gamesResponse.productInfos)
        {
            yield return new ExternalGameData
            {
                Id = product.productId,
                Name = product.displayProductName,
                PluginId = PluginId,
            };
        }
    }

    public IEnumerable<FriendAccountInfo> GetFriends(CancellationToken cancellationToken)
    {
        var userId = UserId;
        string url = $"https://friends.gs.ea.com/friends/2/users/{userId}/friends?names=true";
        var response = downloader.DownloadString(url, cancellationToken: cancellationToken, headerSetter: headers =>
        {
            headers.Add("AuthToken", AuthToken.access_token);
            headers.Add("X-Api-Version", "2");
            headers.Add("X-Application-Key", "Origin");
            headers.Accept.Set("application/json");
        });
        var friendsResponse = JsonConvert.DeserializeObject<FriendsResponse>(response.ResponseContent);

        if (friendsResponse?.entries == null)
            return null;

        return friendsResponse?.entries?.Select(e => new FriendAccountInfo { Id = e.userId.ToString(), Name = e.nickName, Source = this.Source });
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            _authToken = await GetValidAccessTokenAsync();
            var authenticated = _authToken != null;
            logger.Info($"Authenticated: {authenticated}");
            return authenticated;
        }
        catch (NotAuthenticatedException)
        {
            return false;
        }
    }

    private async Task<AuthTokenResponse> GetAccessTokenAsync()
    {
        try
        {
            var response = await webView.DownloadPageTextAsync("https://accounts.ea.com/connect/auth?client_id=ORIGIN_JS_SDK&response_type=token&redirect_uri=nucleus:rest&prompt=none");
            var tokenData = Serialization.FromJson<AuthTokenResponse>(response.Content);
            return tokenData;
        }
        catch (Exception e) { throw new NotAuthenticatedException("Error getting access token", e); }
    }

    private async Task<AuthTokenResponse> GetValidAccessTokenAsync()
    {
        var authToken = await GetAccessTokenAsync();
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
                headers.Authorization = new AuthenticationHeaderValue(authToken.token_type, authToken.access_token);
                headers.Accept.Set("application/json");
            });
            var accountInfo = JsonConvert.DeserializeObject<AccountInfoResponse>(response.ResponseContent);
            return accountInfo.pid.pidId;
        }
        catch (Exception e)
        {
            throw new NotAuthenticatedException("Error getting user ID", e);
        }
    }

    public async Task<bool> IsLoginSuccessAsync(IWebView loginWebView)
    {
        _authToken = null;
        _userId = default;
        return await IsAuthenticatedAsync();
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
