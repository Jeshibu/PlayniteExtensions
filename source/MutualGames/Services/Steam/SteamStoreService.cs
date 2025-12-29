using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using MutualGames.Services.Steam.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using PlayniteExtensions.Common;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MutualGames.Services.Steam;

public class SteamStoreService(IPlayniteAPI playniteApi)
{
    private const string recommendationQueueUrl = "https://store.steampowered.com/explore/";
    private readonly ILogger _logger = LogManager.GetLogger();
    private SteamUserToken? _userTokenFromLogin;

    public async Task<SteamUserToken?> GetSteamUserTokenFromWebViewAsync(IWebView webView)
    {
        _logger.Info("GetSteamUserTokenFromWebViewAsync");
        var url = webView.GetCurrentAddress();
        if (url.Contains("/login"))
        {
            _logger.Info($"Current URL contains /login, canceling: {url}");
            return null;
        }

        var source = await webView.GetPageSourceAsync();
        var doc = await new HtmlParser().ParseAsync(source);
        var configElement = doc.GetElementById("application_config");
        if (configElement == null)
        {
            _logger.Warn("Could not find application config element");
            return null;
        }

        var userConfig = GetJsonAttribute<StoreUserConfig>(configElement, "data-store_user_config");
        var userInfo = GetJsonAttribute<UserInfo>(configElement, "data-userinfo");

        if (userInfo == null || userConfig == null || !userInfo.logged_in)
            return null;

        var token = new SteamUserToken(ulong.Parse(userInfo.steamid), userConfig.webapi_token);

        _logger.Info($"Returning Steam user ID: {token.UserId}");
        return token;
    }

    private T GetJsonAttribute<T>(IElement configElement, string attributeName) where T : class
    {
        var attrValue = configElement.GetAttribute(attributeName);

        if (string.IsNullOrWhiteSpace(attrValue))
        {
            _logger.Warn($"Could not find config attribute: {attributeName}");
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(attrValue);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Config attribute parsing error for {attributeName}: {attrValue}");
            return null;
        }
    }

    public async Task<SteamUserToken> GetAccessTokenAsync()
    {
        using var view = playniteApi.WebViews.CreateOffscreenView();

        view.NavigateAndWait(recommendationQueueUrl);

        return await GetSteamUserTokenFromWebViewAsync(view)
               ?? throw new NotAuthenticatedException();
    }

    public SteamUserToken? Login()
    {
        var view = playniteApi.WebViews.CreateView(600, 720);
        try
        {
            view.LoadingChanged += CloseWhenLoggedIn;
            view.DeleteDomainCookies(".steamcommunity.com");
            view.DeleteDomainCookies("steamcommunity.com");
            view.DeleteDomainCookies("steampowered.com");
            view.DeleteDomainCookies("store.steampowered.com");
            view.DeleteDomainCookies("help.steampowered.com");
            view.DeleteDomainCookies("login.steampowered.com");
            view.Navigate(recommendationQueueUrl);

            _userTokenFromLogin = null;
            view.OpenDialog();
            return _userTokenFromLogin;
        }
        catch (Exception e) when (!Debugger.IsAttached)
        {
            playniteApi.Dialogs.ShowErrorMessage("Error logging into Steam", "");
            _logger.Error(e, "Failed to authenticate user.");
            return null;
        }
        finally
        {
            if (view != null)
            {
                view.LoadingChanged -= CloseWhenLoggedIn;
                view.Dispose();
            }
        }
    }

    private async void CloseWhenLoggedIn(object sender, WebViewLoadingChangedEventArgs e)
    {
        try
        {
            if (e.IsLoading)
                return;

            var view = (IWebView)sender;
            var token = await GetSteamUserTokenFromWebViewAsync(view);
            if (token?.AccessToken != null)
            {
                _userTokenFromLogin = token;
                view.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Failed to check authentication status");
        }
    }
}
