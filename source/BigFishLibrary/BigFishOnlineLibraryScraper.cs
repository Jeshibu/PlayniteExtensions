using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BigFishLibrary;

public class BigFishOnlineLibraryScraper(IPlayniteAPI playniteApi, IWebDownloader downloader)
{
    private readonly ILogger logger = LogManager.GetLogger();
    public const string OrderHistoryUrl = "https://www.bigfishgames.com/order-history.html";

    public IEnumerable<GameMetadata> GetGames()
    {
        string token;
        try
        {
            token = GetTokens();
            logger.Info($"Got token with length {token?.Length}");
        }
        catch (Exception ex)
        {
            logger.Warn(ex, "Error while getting token");
            return [];
        }

        var graphQlGames = GetGamesGraphQL(token);
        var games = graphQlGames.Select(ToGameDetails).ToDictionarySafe(g => g.GameId);
        return games.Values;
    }

    private IEnumerable<Product> GetGamesGraphQL(string token)
    {
        if (token == null)
            throw new NotAuthenticatedException();

        const string url =
            "https://www.bigfishgames.com/graphql?query=query+GetCustomerOrders%28%24filter%3ACustomerOrdersFilterInput%24pageSize%3AInt%21%29%7Bcustomer%7Borders%28filter%3A%24filter+pageSize%3A%24pageSize+scope%3AWEBSITE%29%7B...CustomerOrdersFragment+__typename%7D__typename%7D%7Dfragment+CustomerOrdersFragment+on+CustomerOrders%7Bitems%7Bbilling_address%7Bcity+country_code+firstname+lastname+postcode+region+street+telephone+__typename%7Did+invoices%7Bid+__typename%7Ditems%7Bid+product_name+product_sale_price%7Bcurrency+value+__typename%7Dproduct_sku+product_url_key+selected_options%7Blabel+value+__typename%7Dquantity_ordered+__typename%7Dnumber+order_date+payment_methods%7Bname+type+additional_data%7Bname+value+__typename%7D__typename%7Dshipments%7Bid+tracking%7Bnumber+__typename%7D__typename%7Dshipping_address%7Bcity+country_code+firstname+lastname+postcode+region+street+telephone+__typename%7Dshipping_method+status+state+total%7Bdiscounts%7Bamount%7Bcurrency+value+__typename%7D__typename%7Dgrand_total%7Bcurrency+value+__typename%7Dsubtotal%7Bcurrency+value+__typename%7Dtotal_shipping%7Bcurrency+value+__typename%7Dtotal_tax%7Bcurrency+value+__typename%7D__typename%7D__typename%7Dpage_info%7Bcurrent_page+total_pages+__typename%7Dtotal_count+__typename%7D&operationName=GetCustomerOrders&variables=%7B%22filter%22%3A%7B%7D%2C%22pageSize%22%3A10000%7D";

        var response = downloader.DownloadString(url, headerSetter: GetHeaderSetAction(token), contentType: "application/json", referer: "https://www.bigfishgames.com/");
        logger.Info($"Response ({response.StatusCode}): {response.ResponseContent}");
        var data = JsonConvert.DeserializeObject<LibraryRoot>(response.ResponseContent);
        if (data?.Data?.Customer?.Orders?.Items == null)
            throw new NotAuthenticatedException();

        foreach (var order in data.Data.Customer.Orders.Items)
        foreach (var product in order.Items)
            yield return product;
    }

    private static Action<HttpRequestHeaders> GetHeaderSetAction(string token)
    {
        return headers => { headers.Authorization = new("Bearer", token); };
    }

    private GameMetadata ToGameDetails(Product product)
    {
        logger.Info($"Product: {JsonConvert.SerializeObject(product)}");
        return new GameMetadata
        {
            GameId = product.Sku,
            Name = product.Name,
            Source = new MetadataNameProperty(BigFishLibrary.PluginName),
        };
    }

    private string GetTokens()
    {
        using var webView = playniteApi.WebViews.CreateOffscreenView();
        return GetToken(webView);
    }

    private static string GetToken(IWebView webView)
    {
        const string script = "window.localStorage['M2_VENIA_BROWSER_PERSISTENCE__signin_token']";
        var scriptTask = ExecuteJavaScriptOnPage(webView, OrderHistoryUrl, script, maxAttempts: 2, millisecondsExtraDelay: 3500);
        scriptTask.Wait();
        if (scriptTask.Result == null)
            return null;

        var resultDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(scriptTask.Result.ToString());
        if (resultDict.TryGetValue("value", out var value))
            return value.ToString().Trim('"');

        return null;
    }

    public static bool IsLoggedIn(IWebView webView)
    {
        var token = GetToken(webView);
        return !string.IsNullOrWhiteSpace(token);
    }

    private static async Task<object> ExecuteJavaScriptOnPage(IWebView webView, string url, string script, int maxAttempts = 1, int millisecondsExtraDelay = 0)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            webView.Navigate(url);
            int waitStep = 333, maxWait = 9990;
            for (int elapsed = 0; elapsed < maxWait; elapsed += waitStep)
                if (webView.CanExecuteJavascriptInMainFrame)
                    break;
                else
                    await Task.Delay(waitStep);

            if (!webView.CanExecuteJavascriptInMainFrame)
                continue;

            if (millisecondsExtraDelay > 0)
                await Task.Delay(millisecondsExtraDelay);

            var scriptResult = await webView.EvaluateScriptAsync(script);
            if (scriptResult.Success && scriptResult?.Result != null)
                return scriptResult.Result;
        }

        return null;
    }

    #region json models

    private class LibraryRoot
    {
        public LibraryData Data { get; set; }
    }

    private class LibraryData
    {
        public Customer Customer { get; set; }
    }

    private class Customer
    {
        public Orders Orders { get; set; }
    }

    private class Orders
    {
        [JsonProperty("total_count")] public int TotalCount { get; set; }

        [JsonProperty("page_info")] public PageInfo PageInfo { get; set; }

        public Order[] Items { get; set; }
    }

    private class PageInfo
    {
        [JsonProperty("current_page")] public int CurrentPage { get; set; }

        [JsonProperty("total_pages")] public int TotalPages { get; set; }
    }

    private class Order
    {
        public string Number { get; set; }

        [JsonProperty("order_date")] public string OrderDate { get; set; }

        public string Status { get; set; }

        public Product[] Items { get; set; }

        public DownloadLinks DownloadLinks { get; set; }
    }

    public class Product
    {
        [JsonProperty("product_name")] public string Name { get; set; }

        [JsonProperty("product_type")] public string ProductType { get; set; }

        [JsonProperty("product_url_key")] public string UrlKey { get; set; }

        [JsonProperty("product_sku")] public string Sku { get; set; }
    }

    private class DownloadLinks
    {
        public Link[] Links { get; set; }
    }

    private class Link
    {
        [JsonProperty("product_sku")] public string Sku { get; set; }

        public string Status { get; set; }

        [JsonProperty("link_hash")] public string LinkHash { get; set; }
    }

    #endregion json models
}

internal class NotAuthenticatedException : Exception { }
