using EaLibrary.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    List<GamePlayTime> GetGamePlayTimes(string auth, IEnumerable<string> slugs);
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
    private readonly ILogger _logger = LogManager.GetLogger();
    private readonly string _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    public bool DebugRequests { get; set; }
    public List<string> DebugFilePaths { get; } = [];

    public bool Login()
    {
        var success = false;
        using var webView = webViewFactory.CreateView(500, 700, Color.FromRgb(29, 32, 51));
        webView.DeleteDomainCookiesRegex(@".*\.ea\.com");
        webView.Navigate(LoginUrl);

        webView.LoadingChanged += (_, args) =>
        {
            if (args.IsLoading)
                return;

            var url = webView.GetCurrentAddress();
            if (url == HomeUrl)
            {
                success = true;
                webView.Close();
            }
        };

        webView.OpenDialog(); //blocks until the dialog closes
        return success;
    }

    public bool IsAuthenticated() => GetAuthToken() != null;

    public string GetAuthToken()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        string auth = null;
        var webviewSettings = new WebViewSettings
        {
            JavaScriptEnabled = true,
            PassResourceContentStreamToCallback = false,
            ResourceLoadedCallback = resource =>
            {
                if (cancellationTokenSource.IsCancellationRequested || !resource.Request.Url.StartsWith(GraphQlBaseUrl))
                    return;

                if (resource.Request.Headers.TryGetValue("authorization", out string authHeader))
                {
                    _logger.Info($"Auth header nabbed from {resource.Request.Url}");
                    auth = authHeader.TrimStart("Bearer ");
                    cancellationTokenSource.Cancel();
                }
                else
                {
                    _logger.Info($"No auth header found for {resource.Request.Url}, other headers: {string.Join(", ", resource.Request.Headers.Keys)}");
                }
            }
        };

        using var webView = webViewFactory.CreateOffscreenView(webviewSettings);
        webView.Navigate(DealsUrl);
        try
        {
            Task.Delay(5000, cancellationTokenSource.Token).Wait();
        }
        catch when (cancellationTokenSource.IsCancellationRequested)
        {
        }

        return auth;
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

    public List<GamePlayTime> GetGamePlayTimes(string auth, IEnumerable<string> slugs)
    {
        void HeaderSetter(HttpRequestHeaders headers) => headers.Authorization = new("Bearer", auth);
        var response = downloader.DownloadString(GetPlayTimesUrl(slugs), headerSetter: HeaderSetter);
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
        return GetPersistedQueryUrl("getPreloadedOwnedGames", variables, "5de4178ee7e1f084ce9deca856c74a9e03547a67dfafc0cb844d532fb54ae73d");
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
        if(!DebugRequests)
            return;
        
        var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.Create);
        var filePath = Path.Combine(myDocuments, fileName);
        File.WriteAllText(filePath, response.ResponseContent);
        DebugFilePaths.Add(filePath);
    }
}