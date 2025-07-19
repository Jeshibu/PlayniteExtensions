using HtmlAgilityPack;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamersGateLibrary;

public class GamersGateScraper
{
    private Random random = new Random();
    private ILogger logger = LogManager.GetLogger();
    public int MinDelay { get; set; }
    public int MaxDelay { get; set; }

    public IEnumerable<string> GetAllOrderUrls(IWebViewWrapper downloader)
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

    public void SetWebRequestDelay(int minMilliSeconds, int maxMilliSeconds)
    {
        MinDelay = minMilliSeconds;
        MaxDelay = maxMilliSeconds;
    }

    public Task GetDelayTask()
    {
        if (MaxDelay < 1)
            return Task.CompletedTask;

        var delay = random.Next(MinDelay, MaxDelay);
        return Task.Delay(delay);
    }

    private static string GetOrderPageUrl(int page)
    {
        if (page == 1)
            return "https://www.gamersgate.com/account/orders/";

        return $"https://www.gamersgate.com/account/orders/?page={page}";
    }

    public IEnumerable<string> GetOrderUrls(IWebViewWrapper downloader, int page, out bool hasNextPage)
    {
        hasNextPage = false;

        var url = GetOrderPageUrl(page);
        var response = downloader.DownloadPageSource(url);
        var delayTask = GetDelayTask();

        if (string.IsNullOrWhiteSpace(response))
        {
            logger.Info("Did not get a response from " + url);
            delayTask.Wait();
            return new List<string>();
        }

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(response);

        var pageLinks = doc.DocumentNode.SelectNodes("//div[@class='paginator']//a[@href]");
        if (pageLinks?.Count > 0)
        {
            string nextPageUrl = GetOrderPageUrl(page + 1);
            hasNextPage = pageLinks.Any(l => l.Attributes["href"].Value.GetAbsoluteUrl(url) == nextPageUrl);
        }

        var links = doc.DocumentNode.SelectNodes("//div[@class='table orders-table']//a[@href][1]");
        if (links == null || links.Count == 0)
            return new List<string>();

        var output = links.Select(l => l.Attributes["href"].Value.GetAbsoluteUrl(url)).Distinct().ToList();

        logger.Debug($"Result for order page {page}: {output.Count} orders");
        foreach (var orderUrl in output)
        {
            logger.Debug(orderUrl);
        }

        delayTask.Wait();

        return output;
    }

    public IEnumerable<GameDetails> GetGamesFromOrder(IWebViewWrapper downloader, string orderUrl)
    {
        var output = new List<GameDetails>();

        var response = downloader.DownloadPageSource(orderUrl);
        var delayTask = GetDelayTask();

        if (string.IsNullOrWhiteSpace(response))
        {
            logger.Info("Did not get a response from " + orderUrl);
            delayTask.Wait();
            return output;
        }

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(response);

        var gameNodes = doc.DocumentNode.SelectNodes("//div[@class='content-sub-container order-item-container']");
        if (gameNodes == null)
        {
            logger.Info($"No game nodes found in {orderUrl}");
            return output;
        }

        var orderIdString = doc.DocumentNode.SelectSingleNode("//div[@class='column order-item order-item--date']/a[@class='no-link']")?.InnerText.HtmlDecode().TrimStart('#');
        if (!int.TryParse(orderIdString, out int orderId))
        {
            logger.Info($"Can't parse order id {orderIdString} in {orderUrl}");
            return output;
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
            string key = contentNode.SelectSingleNode("./div[@class='order-item-description']//div[@class='order-item--key-value']")?.InnerText.HtmlDecode();

            output.Add(new GameDetails
            {
                Id = id,
                OrderId = orderId,
                Title = title,
                CoverImageUrl = coverImageUrl,
                DRM = drm,
                UnrevealedKey = unrevealedKey,
                Key = key,
                DownloadUrls = downloadUrls,
            });
        }

        delayTask.Wait();

        return output;
    }

    private List<DownloadUrl> GetGameDownloadUrls(GameDetails game)
    {
        return game.DownloadUrls.Where(u => !u.Description.Contains("Manual") && !u.Description.EndsWith("Demo") && !u.Description.Contains("Patch")).ToList();
    }

    private bool TryGetOrderIdFromUrl(string url, out int id)
    {
        id = 0;
        var segments = url.Split('/', '?', '#');
        var idSegment = segments.FirstOrDefault(s=>s.Length > 0 && s.All(char.IsNumber));
        if (idSegment == null)
            return false;

        id = int.Parse(idSegment);
        return true;
    }

    public LibraryScrapeResults GetAllGames(IWebViewWrapper downloader, params int[] skipOrderIds)
    {
        var orderUrls = GetAllOrderUrls(downloader);
        var output = new LibraryScrapeResults();
        foreach (var orderUrl in orderUrls)
        {
            if (TryGetOrderIdFromUrl(orderUrl, out int orderId))
            {
                output.OrderIds.Add(orderId);

                if (skipOrderIds != null && skipOrderIds.Contains(orderId))
                {
                    logger.Info($"Skipping order {orderId}");
                    continue;
                }
            }

            output.Games.AddRange(GetGamesFromOrder(downloader, orderUrl));
        }

        RemoveSecondaryDownloadUrls(output.Games);

        return output;
    }

    /// <summary>
    /// Remove secondary download URLs that are primary in other games
    /// (for example a PC game would have a PC and Mac download URL, and the Mac version would have the same Mac download URL)
    /// </summary>
    /// <param name="games"></param>
    private void RemoveSecondaryDownloadUrls(List<GameDetails> games)
    {
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
    }
}

public class LibraryScrapeResults
{
    public HashSet<int> OrderIds { get; } = [];

    public List<GameDetails> Games { get; } = [];
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
    public List<DownloadUrl> DownloadUrls { get; set; } = [];
}
