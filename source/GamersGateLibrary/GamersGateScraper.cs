using HtmlAgilityPack;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GamersGateLibrary
{
    public class GamersGateScraper
    {
        private ILogger logger = LogManager.GetLogger();

        public IEnumerable<string> GetAllOrderUrls(IWebDownloader downloader)
        {
            int page = 1;
            var output = new List<string>();

            bool hasNextPage = true;
            while (hasNextPage)
            {
                var orderUrls = GetOrderUrls(downloader, page, out hasNextPage).ToList();
                if (orderUrls == null || !orderUrls.Any())
                    break;
                output.AddRange(orderUrls);
                page++;
            }
            return output;
        }

        private static string GetOrderPageUrl(int page)
        {
            return $"https://www.gamersgate.com/account/orders/?page={page}";
        }

        public IEnumerable<string> GetOrderUrls(IWebDownloader downloader, int page, out bool hasNextPage)
        {
            hasNextPage = false;

            var url = GetOrderPageUrl(page);
            var response = downloader.DownloadString(url);
            if (response.ResponseUrl != url || string.IsNullOrWhiteSpace(response.ResponseContent))
                return new List<string>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response.ResponseContent);

            var pageLinks = doc.DocumentNode.SelectNodes("//div[@class='paginator']//a[@href]");
            if (pageLinks?.Count > 0)
            {
                string nextPageUrl = GetOrderPageUrl(page + 1);
                hasNextPage = pageLinks.Any(l => l.Attributes["href"].Value.GetAbsoluteUrl(url) == nextPageUrl);
            }

            var links = doc.DocumentNode.SelectNodes("//div[@class='table orders-table']//a[@href]");
            if (links == null || links.Count == 0)
                return new List<string>();

            return links.Select(l => l.Attributes["href"].Value.GetAbsoluteUrl(url)).ToHashSet(); //hashset because every URL is on the page twice
        }

        public IEnumerable<GameDetails> GetGamesFromOrder(IWebDownloader downloader, string orderUrl)
        {
            var response = downloader.DownloadString(orderUrl);
            if (response.ResponseUrl != orderUrl || string.IsNullOrWhiteSpace(response.ResponseContent))
                yield break;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response.ResponseContent);

            var gameNodes = doc.DocumentNode.SelectNodes("//div[@class='content-sub-container order-item-container']");
            if (gameNodes == null)
            {
                logger.Info($"No game nodes found in {orderUrl}");
                yield break;
            }

            var orderIdString = doc.DocumentNode.SelectSingleNode("//div[@class='column order-item order-item--date']/a[@class='no-link']")?.InnerText.HtmlDecode().TrimStart('#');
            if (!int.TryParse(orderIdString, out int orderId))
            {
                logger.Info($"Can't parse order id {orderIdString} in {orderUrl}");
                yield break;
            }

            foreach (var g in gameNodes)
            {
                string id = g.SelectSingleNode("./h2[@id]")?.Attributes["id"].Value;
                string title = g.SelectSingleNode("./h2")?.InnerText.HtmlDecode();
                var contentNode = g.SelectSingleNode("./div[@class='order-item-content']");
                if (contentNode == null || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(id))
                    continue;

                var downloadLinks = contentNode.SelectNodes("./div[@class='order-item-description']/div[@class='order-item-download']/a[@href]");
                if (downloadLinks == null || downloadLinks.Count == 0)
                    continue; //if there's no download links it's not DRM free - ignore

                var downloadUrls = downloadLinks.Select(a => new DownloadUrl { Url = a.Attributes["href"].Value.GetAbsoluteUrl(orderUrl), Description = a.InnerText.HtmlDecode() }).ToList();

                var coverImageUrl = contentNode.SelectSingleNode("./div[@class='order-item-image']/img[@src]")?.Attributes["src"].Value;
                if (coverImageUrl?.Contains("noimage") == true)
                    coverImageUrl = null;

                var drm = contentNode.SelectSingleNode("./div[@class='order-item-image']/a[starts-with(@href, '/support/activations/')]")?.Attributes["href"].Value
                    .TrimStart("/support/activations/")
                    .Trim('/');

                bool unrevealedKey = contentNode.SelectSingleNode("./div[@class='order-item-description']/form[@id='show_activation_code_form']") != null;
                string key = contentNode.SelectSingleNode("./div[@class='order-item-description']/div[@class='order-item--key normal']/div[@class='order-item--key-value']")?.InnerText.HtmlDecode();

                yield return new GameDetails
                {
                    Id = id,
                    OrderId = orderId,
                    Title = title,
                    CoverImageUrl = coverImageUrl,
                    DRM = drm,
                    UnrevealedKey = unrevealedKey,
                    Key = key,
                    DownloadUrls = downloadUrls,
                };
            }
        }

        private List<DownloadUrl> GetGameDownloadUrls(GameDetails game)
        {
            return game.DownloadUrls.Where(u => !u.Description.Contains("Manual") && !u.Description.EndsWith("Demo") && !u.Description.Contains("Patch")).ToList();
        }

        public IEnumerable<GameDetails> GetAllGames(IWebDownloader downloader)
        {
            var orderUrls = GetAllOrderUrls(downloader);
            var games = new List<GameDetails>();
            foreach (var orderUrl in orderUrls)
            {
                games.AddRange(GetGamesFromOrder(downloader, orderUrl));
            }

            var singleDownloadUrls = new List<DownloadUrl>();
            foreach (var g in games)
            {
                var downloadUrls = GetGameDownloadUrls(g);
                if (downloadUrls.Count == 1)
                    singleDownloadUrls.Add(downloadUrls[0]);
            }

            foreach (var game in games)
            {
                var gameDownloadUrls = GetGameDownloadUrls(game);
                if (gameDownloadUrls.Count < 2)
                    continue;
                int removed = game.DownloadUrls.RemoveAll(u => singleDownloadUrls.Any(sdu => sdu.Url == u.Url));
                if (removed != 0)
                    logger.Info($"Removed {removed} download URLs from {game.Title} because they're the only download URL for another game entry");
            }

            return games;
        }

        public int? GetLoggedInUserId(IWebDownloader downloader)
        {
            var url = "https://www.gamersgate.com/account/settings/";
            var response = downloader.DownloadString(url, throwExceptionOnErrorResponse: false, customHeaders: new Dictionary<string, string> {
                { "Accept-Language", "en-US,en;q=0.9" },
                { "Upgrade-Insecure-Requests", "1" },
                { "Sec-Fetch-Dest", "document" },
                { "Sec-Fetch-Mode", "navigate" },
                { "Sec-Fetch-Site", "same-origin" },
                { "Sec-Fetch-User", "?1" },
            });
            if (response.ResponseUrl != url || string.IsNullOrWhiteSpace(response.ResponseContent) || (int)response.StatusCode > 399)
                return null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response.ResponseContent);

            var userIdString = doc.DocumentNode.SelectSingleNode("//div[@class='avatar-block']/img[starts-with(@src, '/images/avatar/current/')]")?.Attributes["src"].Value.TrimStart("/images/avatar/current/");
            if (int.TryParse(userIdString, out int output))
                return output;

            return null;
        }
    }

    public class GameDetails
    {
        public string Id { get; set; }
        public int OrderId { get; set; }
        public string Title { get; set; }
        public string CoverImageUrl { get; set; }
        public string DRM { get; set; }
        public string Key { get; set; }
        public bool UnrevealedKey { get; set; }
        public List<DownloadUrl> DownloadUrls { get; set; } = new List<DownloadUrl>();
    }
}
