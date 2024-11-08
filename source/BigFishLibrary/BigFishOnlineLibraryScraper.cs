using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Net;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Linq;
using Playnite.SDK.Models;
using System.Net.Http.Headers;

namespace BigFishLibrary
{
    public class BigFishOnlineLibraryScraper
    {
        private readonly IPlayniteAPI playniteApi;
        private readonly IWebDownloader downloader;
        private readonly ILogger logger = LogManager.GetLogger();
        private const string SessionCookieName = "bfgsess";

        public BigFishOnlineLibraryScraper(IPlayniteAPI playniteApi, IWebDownloader downloader)
        {
            this.playniteApi = playniteApi;
            this.downloader = downloader;
        }

        public IEnumerable<GameMetadata> GetGames()
        {
            BigFishTokens tokens;
            try
            {
                tokens = GetTokens();
                logger.Info($"Got token with length {tokens.Token?.Length}");
                logger.Info($"Got session cookie: {tokens.SessionCookie}");
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Error while getting token");
                return Enumerable.Empty<GameMetadata>();
            }
            var graphQlGames = GetGamesGraphQL(tokens.Token);
            var games = graphQlGames.Select(ToGameDetails).ToDictionarySafe(g => g.GameId);
            try
            {
                var archivedGames = GetGamesArchived(tokens);
                foreach (var archivedGame in archivedGames)
                {
                    if (games.ContainsKey(archivedGame.WrappingId))
                        continue;

                    games.Add(archivedGame.WrappingId, ToGameDetails(archivedGame));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting archived games");
            }
            return games.Values;
        }

        private IEnumerable<Product> GetGamesGraphQL(string token)
        {
            if (token == null)
                throw new NotAuthenticatedException();

            string GetLibraryUrl(int page = 1, int pageSize = 30) => $"https://shop.bigfishgames.com/graphql?query=query+getCustomerPurchaseHistory%28%24pageSize%3AInt%24currentPage%3AInt%24sort%3ACustomerOrderSortInput%24scope%3AScopeTypeEnum%29%7Bcustomer%7Borders%28pageSize%3A%24pageSize+currentPage%3A%24currentPage+sort%3A%24sort+scope%3A%24scope%29%7Bitems%7Bitems%7Bproduct_name+product_type+product_url_key+product_sku+__typename%7Ddownload_links%7Blinks%7Bproduct_sku+status+link_hash+__typename%7D__typename%7Dnumber+order_date+total%7Bgrand_total%7Bvalue+currency+__typename%7D__typename%7Dstatus+__typename%7Dpage_info%7Bcurrent_page+total_pages+__typename%7Dtotal_count+__typename%7D__typename%7D%7D&operationName=getCustomerPurchaseHistory&variables=%7B%22pageSize%22%3A{pageSize}%2C%22currentPage%22%3A{page}%2C%22sort%22%3A%7B%22sort_direction%22%3A%22DESC%22%2C%22sort_field%22%3A%22CREATED_AT%22%7D%2C%22scope%22%3A%22GLOBAL%22%7D";

            int pg = 1, pageTotal = 1;
            do
            {
                var response = downloader.DownloadString(GetLibraryUrl(pg), headerSetter: GetHeaderSetAction(token), contentType: "application/json", referer: "https://www.bigfishgames.com/");
                logger.Info($"Page {pg} response ({response.StatusCode}): {response.ResponseContent}");
                var data = JsonConvert.DeserializeObject<LibraryRoot>(response.ResponseContent);
                if (data?.Data?.Customer == null)
                    throw new NotAuthenticatedException();

                foreach (var order in data.Data.Customer.Orders.Items)
                    foreach (var product in order.Items)
                        yield return product;

                pageTotal = data.Data.Customer.Orders.PageInfo.TotalPages;
                pg++;
            } while (pg <= pageTotal);
        }

        private IEnumerable<ArchivedGame> GetGamesArchived(BigFishTokens tokens)
        {
            if (tokens.SessionCookie == null)
            {
                logger.Error("No session cookie available");
                return Enumerable.Empty<ArchivedGame>();
            }

            if (string.IsNullOrEmpty(tokens?.Token))
            {
                logger.Error("No token");
                return Enumerable.Empty<ArchivedGame>();
            }

            string url = "https://www.bigfishgames.com/bin/servlet/public/archived/purchasehistory?gamesOnly=true&limit=0&offset=0";
            downloader.Cookies.Add(tokens.SessionCookie);
            var response = downloader.DownloadString(url, headerSetter: GetHeaderSetAction(tokens.Token));
            var data = JsonConvert.DeserializeObject<ArchivedLibraryRoot>(response.ResponseContent);
            return data.GameLibrary;
        }

        private static Action<HttpRequestHeaders> GetHeaderSetAction(string token)
        {
            return headers =>
            {
                headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            };
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

        private GameMetadata ToGameDetails(ArchivedGame archivedGame)
        {
            logger.Info($"Archived game: {JsonConvert.SerializeObject(archivedGame)}");
            return new GameMetadata
            {
                GameId = archivedGame.WrappingId,
                Name = archivedGame.Title.HtmlDecode().TrimEnd('™', '®', ' '),
                Source = new MetadataNameProperty(BigFishLibrary.PluginName),
            };
        }

        private class BigFishTokens
        {
            public string Token { get; set; }
            public Cookie SessionCookie { get; set; }
        }

        private BigFishTokens GetTokens()
        {
            using (var webView = playniteApi.WebViews.CreateOffscreenView())
            {
                string token = GetToken(webView);
                var sessionCookie = GetSessionCookie(webView);
                return new BigFishTokens { Token = token, SessionCookie = sessionCookie };
            }
        }

        private static string GetToken(IWebView webView)
        {
            const string url = "https://www.bigfishgames.com/us/en/store/my-orders-history.html";
            const string script = "window.localStorage['M2_VENIA_BROWSER_PERSISTENCE__signin_token']";
            var scriptTask = ExecuteJavaScriptOnPage(webView, url, script, maxAttempts: 2, millisecondsExtraDelay: 4000);
            scriptTask.Wait();
            if ((scriptTask?.Result) == null)
                return null;

            var resultDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(scriptTask.Result.ToString());
            if (resultDict.TryGetValue("value", out var value))
                return value.ToString().Trim('"');

            return null;
        }

        private static Cookie GetSessionCookie(IWebView webView)
        {
            var cookies = webView.GetCookies();
            var cookie = cookies.FirstOrDefault(c => c.Domain == ".bigfishgames.com" && c.Name == SessionCookieName);
            if (cookie == null)
                return null;
            return new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain);
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

        private class ArchivedLibraryRoot
        {
            public List<ArchivedGame> GameLibrary { get; set; } = new List<ArchivedGame>();
        }

        private class ArchivedGame
        {
            [JsonProperty("transaction_id")]
            public long TransactionId { get; set; }

            public long Date { get; set; }

            [JsonProperty("revoke_date")]
            public long RevokeDate { get; set; }

            [JsonProperty("refund_date")]
            public long RefundDate { get; set; }

            public bool IsActivationAvailable { get; set; }

            [JsonProperty("download_link")]
            public string DownloadLink { get; set; }

            [JsonProperty("language_id")]
            public int LanguageId { get; set; }

            [JsonProperty("game_id")]
            public int GameId { get; set; }

            [JsonProperty("wrapping_id")]
            public string WrappingId { get; set; }

            [JsonProperty("image_6040")]
            public string Image6040 { get; set; }

            [JsonProperty("image_feature")]
            public string ImageFeature { get; set; }

            public string Title { get; set; }

            [JsonProperty("asset_folder_name")]
            public string AssetFolderName { get; set; }
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
