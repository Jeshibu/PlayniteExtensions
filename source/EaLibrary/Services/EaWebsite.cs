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

// ReSharper disable MethodSupportsCancellation

namespace EaLibrary.Services;

public interface IEaWebsite
{
    bool Login();
    bool IsAuthenticated();
    string GetAuthToken();
    List<OwnedGameProduct> GetOwnedGames(string auth);
    IEnumerable<LegacyOffer> GetLegacyOffers(string[] offerIds);
}

public class EaWebsite(IWebViewFactory webViewFactory, IWebDownloader downloader) : IEaWebsite
{
    private const string HomeUrl = "https://www.ea.com/";
    private const string LoginUrl = "https://www.ea.com/login";
    private const string DealsUrl = "https://www.ea.com/sales/deals";
    private const string AccountUrl = "https://myaccount.ea.com/am/ui/account-information";
    private const string GraphQlBaseUrl = "https://service-aggregation-layer.juno.ea.com/graphql";
    private readonly ILogger _logger = LogManager.GetLogger();

    public bool Login()
    {
        var success = false;
        using var webView = webViewFactory.CreateView(500, 700, Colors.DarkBlue);
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

                _logger.Info(resource.Request.Url);

                if (resource.Request.Headers.TryGetValue("authorization", out string authHeader))
                {
                    _logger.Info($"Auth header: {authHeader}");
                    auth = authHeader.TrimStart("Bearer ");
                    cancellationTokenSource.Cancel();
                }
                else
                {
                    _logger.Warn($"No auth header found for {resource.Request.Url}, other headers: {string.Join(", ", resource.Request.Headers.Keys)}");
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
            var response = downloader.DownloadString(GetGamesUrl(DefaultLimit, offset), headerSetter: HeaderSetter);

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

    public const int DefaultLimit = 500;

    public static string GetGamesUrl(int limit = DefaultLimit, string offset = "0") =>
        $$$"""{{{GraphQlBaseUrl}}}?operationName=getPreloadedOwnedGames&variables={"isMac":false,"addFieldsToPreloadGames":true, "locale":"en","limit":{{{limit}}},"next":"{{{offset}}}","type":["DIGITAL_FULL_GAME","PACKAGED_FULL_GAME"],"entitlementEnabled":true,"storefronts":["EA","STEAM","EPIC"],"ownershipMethods":["UNKNOWN","ASSOCIATION","PURCHASE","REDEMPTION","GIFT_RECEIPT","ENTITLEMENT_GRANT","DIRECT_ENTITLEMENT","PRE_ORDER_PURCHASE","VAULT","XGP_VAULT","STEAM","STEAM_VAULT","STEAM_SUBSCRIPTION","EPIC","EPIC_VAULT","EPIC_SUBSCRIPTION"],"platforms":["PC"]}&extensions={"persistedQuery":{"version":1,"sha256Hash":"5de4178ee7e1f084ce9deca856c74a9e03547a67dfafc0cb844d532fb54ae73d"}}""";

    public IEnumerable<LegacyOffer> GetLegacyOffers(string[] offerIds)
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
        var response = downloader.PostAsync(GraphQlBaseUrl, dataString, contentType: "application/json").Result;
        var responseObj = JsonConvert.DeserializeObject<GraphQlResponseRoot<LegacyOffersData>>(response.ResponseContent);
        return responseObj.data.legacyOffers;
    }
}