using EaLibrary.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

// ReSharper disable MethodSupportsCancellation

namespace EaLibrary.Services;

public interface IEaWebsite
{
    bool Login();
    bool IsAuthenticated();
    string GetAuthToken();
    List<OwnedGameProduct> GetOwnedGames(string auth);
    List<GamePlayTime> GetGamePlayTimes(string auth, string[] slugs);
    Task<LegacyOffer[]> GetLegacyOffersAsync(string[] offerIds);

    bool DebugRequests { get; set; }
    List<string> DebugFilePaths { get; }
}

public class EaWebsite(IWebViewFactory webViewFactory, IWebDownloader downloader) : IEaWebsite
{
    private const string HomeUrl = "https://www.ea.com/";
    private const string LoginUrl = "https://www.ea.com/login";
    private const string DealsUrl = "https://www.ea.com/sales/deals";
    private const string GraphQlBaseUrl = "https://service-aggregation-layer.juno.ea.com/graphql";
    private const string NucleusAuthUrl = "https://accounts.ea.com/connect/auth?client_id=ORIGIN_JS_SDK&redirect_uri=nucleus:rest&response_type=token&locale=en_US";
    private readonly ILogger _logger = LogManager.GetLogger();
    private readonly string _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    private string _cachedToken;
    public bool DebugRequests { get; set; }
    public List<string> DebugFilePaths { get; } = [];

    public bool Login()
    {
        var success = false;
        _cachedToken = null;
        using var webView = webViewFactory.CreateView(500, 700, Color.FromRgb(29, 32, 51));
        webView.DeleteDomainCookiesRegex(@".*\.ea\.com");
        webView.Navigate(LoginUrl);

        void OnLoadingChanged(object _, WebViewLoadingChangedEventArgs args)
        {
            if (args.IsLoading) return;

            var url = webView.GetCurrentAddress();
            if (url == HomeUrl || (url != null && url.StartsWith(HomeUrl) && !url.Contains("login")))
            {
                success = true;
                webView.Close();
            }
        }

        webView.LoadingChanged += OnLoadingChanged;
        webView.OpenDialog();
        webView.LoadingChanged -= OnLoadingChanged;

        if (success)
            _cachedToken = FetchTokenViaCookies();

        return success;
    }

    public bool IsAuthenticated() => GetAuthToken() != null;

    public string GetAuthToken()
    {
        if (!string.IsNullOrEmpty(_cachedToken))
            return _cachedToken;

        // Try fetching a fresh token using persisted session cookies
        _cachedToken = FetchTokenViaCookies();
        return _cachedToken;
    }

    /// <summary>
    /// Fetches an access token from EA using the sid/remid cookies in the shared CEF cookie store.
    /// Uses the ORIGIN_JS_SDK client with nucleus:rest redirect which returns the token as JSON.
    /// </summary>
    private string FetchTokenViaCookies()
    {
        try
        {
            var webviewSettings = new WebViewSettings { JavaScriptEnabled = false };
            using var webView = webViewFactory.CreateOffscreenView(webviewSettings);
            webView.Navigate(HomeUrl);
            Task.Delay(2000).Wait();

            var cookies = webView.GetCookies();
            if (cookies == null)
                return null;

            var sidCookie = cookies.FirstOrDefault(c => c.Name == "sid" && c.Domain != null && c.Domain.Contains("ea.com"));
            var remidCookie = cookies.FirstOrDefault(c => c.Name == "remid" && c.Domain != null && c.Domain.Contains("ea.com"));

            if (sidCookie == null)
            {
                _logger.Info("No EA session cookie found - login required");
                return null;
            }

            var cookieHeader = $"sid={sidCookie.Value}";
            if (remidCookie != null)
                cookieHeader += $"; remid={remidCookie.Value}";

            return RequestTokenFromEa(cookieHeader);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error fetching token via cookies");
            return null;
        }
    }

    private string RequestTokenFromEa(string cookieHeader)
    {
        try
        {
            var request = WebRequest.CreateHttp(NucleusAuthUrl);
            request.Method = "GET";
            request.AllowAutoRedirect = false;
            request.Headers.Add("Cookie", cookieHeader);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

            using var response = (HttpWebResponse)request.GetResponse();
            using var reader = new StreamReader(response.GetResponseStream());
            var body = reader.ReadToEnd();

            var tokenObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
            if (tokenObj != null && tokenObj.ContainsKey("access_token"))
            {
                _logger.Info("Successfully obtained EA access token via nucleus:rest flow");
                return tokenObj["access_token"].ToString();
            }

            _logger.Warn($"Unexpected token response: {body}");
        }
        catch (WebException wex) when (wex.Response is HttpWebResponse errResp)
        {
            using var reader = new StreamReader(errResp.GetResponseStream());
            _logger.Error($"EA token request failed ({(int)errResp.StatusCode}): {reader.ReadToEnd()}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error requesting EA token");
        }

        return null;
    }

    public List<OwnedGameProduct> GetOwnedGames(string auth)
    {
        void HeaderSetter(HttpRequestHeaders headers) => headers.Authorization = new("Bearer", auth);
        List<OwnedGameProduct> output = [];
        string offset = "0";

        do
        {
            var response = downloader.DownloadString(GetGamesUrl(offset), headerSetter: HeaderSetter);
            SaveResponse(response, $"ea-{_version}-owned-games-{offset}.json");

            var root = JsonConvert.DeserializeObject<GraphQlResponseRoot<OwnedGamesData>>(response.ResponseContent);
            var ownedGames = root?.data?.me?.ownedGameProducts;

            if (ownedGames?.items != null)
            {
                output.AddRange(ownedGames.items);
                offset = ownedGames.next;
            }
            else
            {
                offset = null;
            }
        } while (offset != null);

        return output;
    }

    public List<GamePlayTime> GetGamePlayTimes(string auth, string[] slugs)
    {
        void HeaderSetter(HttpRequestHeaders headers) => headers.Authorization = new("Bearer", auth);
        var response = downloader.DownloadString(GetPlayTimesUrl(slugs), headerSetter: HeaderSetter);
        SaveResponse(response, $"ea-{_version}-playtimes-{string.Join("_", "slugs")}.json");
        var root = JsonConvert.DeserializeObject<GraphQlResponseRoot<GetGamesPlayTimesRoot>>(response.ResponseContent);
        return root?.data?.me?.recentGames?.items.ToList();
    }

    public static string GetGamesUrl(string offset = "0", int limit = 500)
    {
        var variables = new
        {
            isMac = false,
            addFieldsToPreloadGames = true,
            locale = "en",
            limit,
            next = offset,
            type = new[] { "DIGITAL_FULL_GAME", "PACKAGED_FULL_GAME" },
            entitlementEnabled = true,
            storefronts = new[] { "EA", "STEAM", "EPIC" },
            ownershipMethods = new[]
            {
                "UNKNOWN", "ASSOCIATION", "PURCHASE", "REDEMPTION", "GIFT_RECEIPT", "ENTITLEMENT_GRANT", "DIRECT_ENTITLEMENT", "PRE_ORDER_PURCHASE",
                "VAULT", "XGP_VAULT", "STEAM", "STEAM_VAULT", "STEAM_SUBSCRIPTION", "EPIC", "EPIC_VAULT", "EPIC_SUBSCRIPTION"
            },
            platforms = new[] { "PC" }
        };
        return GetPersistedQueryUrl("getPreloadedOwnedGames", variables, "779f1cd1355699752e20c0b3877847f4e3010ef5de131c248e98f8eff84f0718");
    }

    public static string GetPlayTimesUrl(IEnumerable<string> gameSlugs) => GetPersistedQueryUrl("GetGamePlayTimes", new { gameSlugs }, "3f09b35e06b75c74d8ec3e520a598ebb5e2992b1e1268b6dd3b8ed99b9fafb29");

    private static string GetPersistedQueryUrl(string operation, object variables, string hash)
    {
        var variablesJson = JsonConvert.SerializeObject(variables);
        var variablesQueryString = WebUtility.UrlEncode(variablesJson);
        return $$$"""{{{GraphQlBaseUrl}}}?operationName={{{operation}}}&variables={{{variablesQueryString}}}&extensions={"persistedQuery":{"version":1,"sha256Hash":"{{{hash}}}"}}""";
    }

    public async Task<LegacyOffer[]> GetLegacyOffersAsync(string[] offerIds)
    {
        const string query = """
                             query getLegacyCatalogDefs($offerIds: [String!]!, $locale: Locale) {
                               legacyOffers(offerIds: $offerIds, locale: $locale) {
                                 offerId: id
                                 contentId
                                 basePlatform
                                 primaryMasterTitleId
                                 mdmProjectNumber
                                 achievementSetOverride
                                 gameLauncherURL
                                 gameLauncherURLClientID
                                 stagingKeyPath
                                 mdmTitleIds
                                 multiplayerId
                                 executePathOverride
                                 installationDirectory
                                 installCheckOverride
                                 monitorPlay
                                 displayName
                                 displayType
                                 igoBrowserDefaultUrl
                                 executeParameters
                                 softwareLocales
                                 dipManifestRelativePath
                                 metadataInstallLocation
                                 distributionSubType
                                 downloads {
                                   igoApiEnabled
                                   downloadType
                                   version
                                   executeElevated
                                   buildReleaseVersion
                                   buildLiveDate
                                   buildMetaData
                                   gameVersion
                                   treatUpdatesAsMandatory
                                   enableDifferentialUpdate
                                 }
                                 locale
                                 greyMarketControls
                                 isDownloadable
                                 isPreviewDownload
                                 downloadStartDate
                                 releaseDate
                                 useEndDate
                                 subscriptionUnlockDate
                                 subscriptionUseEndDate
                                 softwarePlatform
                                 softwareId
                                 downloadPackageType
                                 installerPath
                                 processorArchitecture
                                 macBundleID
                                 gameEditionTypeFacetKeyRankDesc
                                 appliedCountryCode
                                 cloudSaveConfigurationOverride
                                 firstParties{
                                     partner
                                     partnerId
                                     partnerIdType
                                 }
                                 suppressedOfferIds
                               }
                             }
                             """;

        var data = new { query, operationName = "getLegacyCatalogDefs", variables = new { locale = "DEFAULT", offerIds } };
        var dataString = JsonConvert.SerializeObject(data);
        var response = await downloader.PostAsync(GraphQlBaseUrl, dataString, contentType: "application/json");
        SaveResponse(response, $"ea-{_version}-legacy-offers.json");
        var responseObj = JsonConvert.DeserializeObject<GraphQlResponseRoot<LegacyOffersData>>(response.ResponseContent);
        return responseObj.data.legacyOffers;
    }

    private void SaveResponse(DownloadStringResponse response, string fileName)
    {
        if (!DebugRequests)
            return;

        var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.Create);
        var filePath = Path.Combine(myDocuments, fileName);
        File.WriteAllText(filePath, response.ResponseContent);
        DebugFilePaths.Add(filePath);
    }
}
