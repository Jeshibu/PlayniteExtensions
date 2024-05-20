using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Net;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace BigFishLibrary
{
    public class BigFishOnlineLibraryScraper
    {
        private readonly IPlayniteAPI playniteApi;
        private readonly IWebDownloader downloader;
        private readonly ILogger logger = LogManager.GetLogger();

        public BigFishOnlineLibraryScraper(IPlayniteAPI playniteApi, IWebDownloader downloader)
        {
            this.playniteApi = playniteApi;
            this.downloader = downloader;
        }

        private string GetLibraryUrl(int page = 1, int pageSize = 30) => $"https://shop.bigfishgames.com/graphql?query=query+getCustomerPurchaseHistory%28%24pageSize%3AInt%24currentPage%3AInt%24sort%3ACustomerOrderSortInput%24scope%3AScopeTypeEnum%29%7Bcustomer%7Borders%28pageSize%3A%24pageSize+currentPage%3A%24currentPage+sort%3A%24sort+scope%3A%24scope%29%7Bitems%7Bitems%7Bproduct_name+product_type+product_url_key+product_sku+__typename%7Ddownload_links%7Blinks%7Bproduct_sku+status+link_hash+__typename%7D__typename%7Dnumber+order_date+total%7Bgrand_total%7Bvalue+currency+__typename%7D__typename%7Dstatus+__typename%7Dpage_info%7Bcurrent_page+total_pages+__typename%7Dtotal_count+__typename%7D__typename%7D%7D&operationName=getCustomerPurchaseHistory&variables=%7B%22pageSize%22%3A{pageSize}%2C%22currentPage%22%3A{page}%2C%22sort%22%3A%7B%22sort_direction%22%3A%22DESC%22%2C%22sort_field%22%3A%22CREATED_AT%22%7D%2C%22scope%22%3A%22GLOBAL%22%7D";
        public IEnumerable<Product> GetGames()
        {

            string token;
            try
            {
                token = GetToken();
                logger.Info($"Got token with length {token?.Length}");
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Error while getting token");
                yield break;
            }
            if (token == null)
                throw new NotAuthenticatedException();

            Action<WebHeaderCollection> headerSetter = (WebHeaderCollection header) =>
            {
                header.Set(HttpRequestHeader.Authorization, $"Bearer {token}");
                header.Set("origin", "https://www.bigfishgames.com");
                header.Set(HttpRequestHeader.CacheControl, "no-cache");
            };

            int page = 1, pageTotal = 1;
            do
            {
                var response = downloader.DownloadString(GetLibraryUrl(page), headerSetter: headerSetter, contentType: "application/json", referer: "https://www.bigfishgames.com/");
                logger.Info($"Page {page} response ({response.StatusCode}): {response.ResponseContent}");
                var data = JsonConvert.DeserializeObject<LibraryRoot>(response.ResponseContent);
                if (data?.Data?.Customer == null)
                    throw new NotAuthenticatedException();

                foreach (var order in data.Data.Customer.Orders.Items)
                    foreach (var product in order.Items)
                        yield return product;

                pageTotal = data.Data.Customer.Orders.PageInfo.TotalPages;
                page++;
            } while (page <= pageTotal);
        }

        private string GetToken()
        {
            const string url = "https://www.bigfishgames.com/us/en/store/my-orders-history.html";
            const string script = "window.localStorage['M2_VENIA_BROWSER_PERSISTENCE__signin_token']";
            var scriptTask = ExecuteJavaScriptOnPage(url, script, maxAttempts: 2, millisecondsExtraDelay: 4000);
            scriptTask.Wait();
            if ((scriptTask?.Result) == null)
                return null;

            var resultDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(scriptTask.Result.ToString());
            if (resultDict.TryGetValue("value", out var value))
                return value.ToString().Trim('"');

            return null;
        }

        private async Task<object> ExecuteJavaScriptOnPage(string url, string script, int maxAttempts = 1, int millisecondsExtraDelay = 0)
        {
            using (var webView = playniteApi.WebViews.CreateOffscreenView())
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
            [JsonProperty("total_count")]
            public int TotalCount { get; set; }

            [JsonProperty("page_info")]
            public PageInfo PageInfo { get; set; }

            public Order[] Items { get; set; }
        }

        private class PageInfo
        {
            [JsonProperty("current_page")]
            public int CurrentPage { get; set; }

            [JsonProperty("total_pages")]
            public int TotalPages { get; set; }
        }

        private class Order
        {
            public string Number { get; set; }

            [JsonProperty("order_date")]
            public string OrderDate { get; set; }

            public string Status { get; set; }

            public Product[] Items { get; set; }

            public DownloadLinks DownloadLinks { get; set; }
        }

        public class Product
        {
            [JsonProperty("product_name")]
            public string Name { get; set; }

            [JsonProperty("product_type")]
            public string ProductType { get; set; }

            [JsonProperty("product_url_key")]
            public string UrlKey { get; set; }

            [JsonProperty("product_sku")]
            public string Sku { get; set; }
        }

        private class DownloadLinks
        {
            public Link[] Links { get; set; }
        }

        private class Link
        {
            [JsonProperty("product_sku")]
            public string Sku { get; set; }

            public string Status { get; set; }

            [JsonProperty("link_hash")]
            public string LinkHash { get; set; }
        }
        #endregion json models
    }

    [Serializable]
    internal class NotAuthenticatedException : Exception
    {
        public NotAuthenticatedException()
        {
        }

        public NotAuthenticatedException(string message) : base(message)
        {
        }

        public NotAuthenticatedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotAuthenticatedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
