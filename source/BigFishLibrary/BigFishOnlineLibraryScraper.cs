using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.WebViewModels;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BigFishLibrary;

public class BigFishOnlineLibraryScraper(IPlayniteAPI playniteApi)
{
    private readonly ILogger logger = LogManager.GetLogger();
    private const string OrderHistoryUrl = "https://www.bigfishgames.com/order-history.html";

    public IEnumerable<GameMetadata> GetGames()
    {
        var graphQlGames = GetGamesIntercept(CancellationToken.None);
        var orders = graphQlGames?.Data?.Customer?.Orders.Items;
        if (orders is null)
            throw new NotAuthenticatedException();

        var games = orders
                    .SelectMany(i => i.Items)
                    .Select(ToGameDetails)
                    .ToDictionarySafe(g => g.GameId);

        return games.Values;
    }

    private LibraryRoot GetGamesIntercept(CancellationToken cancellationToken)
    {
        var orderHistoryResponse = new TaskCompletionSource<string>();
        using var webView = playniteApi.WebViews.CreateOffscreenView(new()
        {
            JavaScriptEnabled = true, PassResourceContentStreamToCallback = true,
            ShouldPassResourceContentFunc = IsOrderHistoryCall,
            ResourceLoadedCallback = GetResourceCallback(orderHistoryResponse),
        });

        webView.Navigate(OrderHistoryUrl);
        var completedTask = Task.WhenAny(orderHistoryResponse.Task, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken)).GetAwaiter().GetResult();

        if (completedTask != orderHistoryResponse.Task)
            return null;

        return JsonConvert.DeserializeObject<LibraryRoot>(orderHistoryResponse.Task.Result);
    }

    public bool IsAuthenticated(CancellationToken cancellationToken = default) => GetGamesIntercept(cancellationToken) != null;

    public bool Authenticate()
    {
        var orderHistoryResponse = new TaskCompletionSource<string>();
        using var view = playniteApi.WebViews.CreateView(new()
        {
            JavaScriptEnabled = true, PassResourceContentStreamToCallback = true,
            ShouldPassResourceContentFunc = IsOrderHistoryCall,
            ResourceLoadedCallback = GetResourceCallback(orderHistoryResponse),
            WindowWidth = 675,
            WindowHeight = 675,
            WindowBackground = Colors.White,
        });

        view.LoadingChanged += CloseWhenLoggedIn;
        try
        {
            view.DeleteDomainCookies(".bigfishgames.com");
            view.DeleteDomainCookies("bigfishgames.com");
            view.DeleteDomainCookies(".www.bigfishgames.com");
            view.DeleteDomainCookies("www.bigfishgames.com");
            view.Navigate(OrderHistoryUrl);

            view.OpenDialog();

            return orderHistoryResponse.Task.IsCompleted
                   && orderHistoryResponse.Task.Result != null;
        }
        catch (Exception e)
        {
            playniteApi.Dialogs.ShowErrorMessage("Error logging in to Big Fish Games", "");
            logger.Error(e, "Failed to authenticate user.");
            return false;
        }
        finally
        {
            view.LoadingChanged -= CloseWhenLoggedIn;
        }

        void CloseWhenLoggedIn(object sender, WebViewLoadingChangedEventArgs e)
        {
            var webView = (IWebView)sender;
            if (orderHistoryResponse.Task.IsCompleted)
                webView.Close();
        }
    }

    private static bool IsOrderHistoryCall(ShouldPassResourceContentFuncArgs a) => IsOrderHistoryCall(a.Request, a.Response);
    private static bool IsOrderHistoryCall(WebViewResourceLoadedCallback a) => IsOrderHistoryCall(a.Request, a.Response);

    private static bool IsOrderHistoryCall(Request request, Response response) => response.StatusCode == 200
                                                                                  && request.Url.StartsWith("https://www.bigfishgames.com/graphql?query=query+GetCustomerOrders");

    private Action<WebViewResourceLoadedCallback> GetResourceCallback(TaskCompletionSource<string> orderHistoryResponse)
    {
        return ResourceCallback;

        void ResourceCallback(WebViewResourceLoadedCallback call)
        {
            try
            {
                if (call.Request.Url.Contains("sign-in"))
                    return;

                if (!IsOrderHistoryCall(call))
                    return;

                if (call.ResponseContent is not { CanSeek: true, CanRead: true })
                {
                    logger.Error($"Can't read/seek response content for {call.Request.Url}");
                    orderHistoryResponse.SetResult(null);
                    return;
                }

                call.ResponseContent.Seek(0, SeekOrigin.Begin);
                using var streamReader = new StreamReader(call.ResponseContent, Encoding.UTF8);
                string responseContent = streamReader.ReadToEnd();

                orderHistoryResponse.SetResult(responseContent);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error getting order history");
            }
        }
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

internal class NotAuthenticatedException : Exception;
