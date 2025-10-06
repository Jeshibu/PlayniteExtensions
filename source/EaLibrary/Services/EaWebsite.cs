using EaLibrary.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace EaLibrary.Services;

public class EaWebsite(IWebViewFactory webViewFactory, IWebDownloader downloader)
{
    private const string AccountUrl = "https://myaccount.ea.com/am/ui/account-information";
    private const string GraphQlBaseUrl = "https://service-aggregation-layer.juno.ea.com/graphql";
    private static string[] SubscriptionTypes = ["VAULT", "XGP_VAULT", "STEAM", "STEAM_VAULT", "STEAM_SUBSCRIPTION", "EPIC", "EPIC_VAULT", "EPIC_SUBSCRIPTION"];

    public bool Login()
    {
        var success = false;
        using var webView = webViewFactory.CreateView(500, 700, Colors.DarkBlue);
        webView.DeleteDomainCookies(".ea.com");
        webView.DeleteDomainCookies(".myaccount.ea.com");
        webView.Navigate(AccountUrl);

        webView.LoadingChanged += (_, args) =>
        {
            if (args.IsLoading)
                return;

            var url = webView.GetCurrentAddress();
            if (url == AccountUrl)
            {
                success = true;
                webView.Close();
            }
        };

        webView.OpenDialog(); //blocks until the dialog closes
        return success;
    }

    public bool IsAuthenticated()
    {
        bool authenticated = false;
        var cancellationTokenSource = new CancellationTokenSource();
        using var webView = webViewFactory.CreateOffscreenView();
        webView.LoadingChanged += (_, args) =>
        {
            if (args.IsLoading || cancellationTokenSource.IsCancellationRequested)
                return;

            var url = webView.GetCurrentAddress();
            if (url == AccountUrl)
            {
                authenticated = true;
                cancellationTokenSource.Cancel();
            }

            if (url.Contains("/login"))
            {
                authenticated = false;
                cancellationTokenSource.Cancel();
            }
        };
        webView.Navigate(AccountUrl);
        Task.Delay(5000, cancellationTokenSource.Token).Wait(cancellationTokenSource.Token);
        return authenticated;
    }

    public string GetAuthToken()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        string auth = null;
        var webviewSettings = new WebViewSettings
        {
            JavaScriptEnabled = true,
            ResourceLoadedCallback = callback =>
            {
                if (!cancellationTokenSource.IsCancellationRequested
                    && callback.Request.Headers.TryGetValue("Authorization", out var authHeader)
                    && authHeader.StartsWith("Bearer "))
                {
                    auth = authHeader.TrimStart("Bearer ");
                    cancellationTokenSource.Cancel();
                }
            }
        };

        using var webView = webViewFactory.CreateOffscreenView(webviewSettings);
        webView.Navigate("https://www.ea.com/sales/deals");
        Task.Delay(5000, cancellationTokenSource.Token).Wait(cancellationTokenSource.Token);
        return auth;
    }

    public List<Items> GetOwnedGames(string auth)
    {
        List<Items> output = [];
        string offset = "0";

        do
        {
            var response = downloader.DownloadString(GetGamesUrl(100, offset), headerSetter: HeaderSetter);
            var root = JsonConvert.DeserializeObject<OwnedGamesRoot>(response.ResponseContent);
            var ownedGames = root?.data?.me?.ownedGameProducts;
            if (ownedGames != null)
            {
                output.AddRange(ownedGames.items);
                offset = ownedGames.next;
            }
        } while (offset != null);

        return output;
        
        void HeaderSetter(HttpRequestHeaders headers) => headers.Authorization = new("Bearer", auth);
    }

    public static string GetGamesUrl(int limit = 100, string offset = "0") =>
        $$"""{{GraphQlBaseUrl}}?operationName=getPreloadedOwnedGames&variables={"isMac":false,"addFieldsToPreloadGames":true, "locale":"en","limit":{{limit}},"next":"{{offset}}","type":["DIGITAL_FULL_GAME","PACKAGED_FULL_GAME"],"entitlementEnabled":true,"storefronts":["EA","STEAM","EPIC"],"ownershipMethods":["UNKNOWN","ASSOCIATION","PURCHASE","REDEMPTION","GIFT_RECEIPT","ENTITLEMENT_GRANT","DIRECT_ENTITLEMENT","PRE_ORDER_PURCHASE","VAULT","XGP_VAULT","STEAM","STEAM_VAULT","STEAM_SUBSCRIPTION","EPIC","EPIC_VAULT","EPIC_SUBSCRIPTION"],"platforms":["PC"]}&extensions={"persistedQuery":{"version":1,"sha256Hash":"5de4178ee7e1f084ce9deca856c74a9e03547a67dfafc0cb844d532fb54ae73d"}}""";
}
