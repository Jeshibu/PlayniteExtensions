using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace GiantBombMetadata.Api;

public class GiantBombScraper(IWebDownloader downloader, IPlatformUtility platformUtility)
{
    private static string GetFirstEntityGamePageUrl(string entityUrl) => entityUrl + "games/";
    private static string GetFirstGlobalGamesFilterPageUrl(string entityType, int entityId) => $"https://www.giantbomb.com/games/?game_filter%5B{entityType}%5D={entityId}";

    public IEnumerable<GameDetails> GetGamesForEntity(string entityUrl, GlobalProgressActionArgs progressArgs = null)
    {
        var firstPageUrl = GetFirstEntityGamePageUrl(entityUrl);
        return GetPaginatedGames(firstPageUrl, "li.related-game > a", progressArgs);
    }

    public IEnumerable<GameDetails> GetGamesForGenreOrTheme(string entityType, int entityId, GlobalProgressActionArgs progressArgs = null)
    {
        var firstPageUrl = GetFirstGlobalGamesFilterPageUrl(entityType, entityId);
        return GetPaginatedGames(firstPageUrl, "ul.editorial > li > a");
    }

    public IEnumerable<GiantBombSearchResultItem> SearchObjects(string query)
    {
        var url = "https://www.giantbomb.com/search/?i=object&q=" + HttpUtility.UrlEncode(query);
        var response = downloader.DownloadString(url);
        var htmlDocument = new HtmlParser().Parse(response.ResponseContent);
        var items = htmlDocument.QuerySelectorAll("ul.search-results a");
        foreach (var item in items)
        {
            var itemUrl = item.Attributes["href"].Value;

            var itemId = giantBombItemIdRegex.Match(itemUrl).Value;

            yield return new GiantBombSearchResultItem
            {
                Name = item.QuerySelector(".title")?.TextContent?.Trim(),
                Deck = item.QuerySelector(".deck")?.TextContent?.Trim(),
                ResourceType = "object",
                SiteDetailUrl = itemUrl,
                Guid = itemId,
            };
        }
    }

    private readonly Regex giantBombItemIdRegex = new("3[0-9]{3}-[0-9]+", RegexOptions.Compiled);

    private static PaginationInfo GetPaginationInfo(string url, IHtmlDocument htmlDocument)
    {
        var pagination = htmlDocument.QuerySelector("ul.paginate");
        if (pagination == null) return new PaginationInfo { CurrentPage = 1, TotalPages = 1 };

        var output = new PaginationInfo();
        var currentPageString = pagination.QuerySelector("li.on > a").TextContent;
        var relativeNextPageUrl = pagination.QuerySelector("li.next > a")?.GetAttribute("href");
        if (int.TryParse(currentPageString, out int currentPage))
            output.CurrentPage = currentPage;
        if (relativeNextPageUrl != null)
            output.NextPageUrl = new Uri(new Uri(url), relativeNextPageUrl).AbsoluteUri;

        var allLinks = pagination.QuerySelectorAll("li > a")?.ToList();
        if (allLinks is { Count: > 2 })
        {
            string totalPagesString = allLinks[allLinks.Count - 2].TextContent;
            if (int.TryParse(totalPagesString, out int totalPages))
                output.TotalPages = totalPages;
        }
        return output;
    }

    private IEnumerable<GameDetails> GetGamesFromDocument(string url, IHtmlDocument htmlDocument, string gameElementSelector)
    {
        var gameElements = htmlDocument.QuerySelectorAll(gameElementSelector);
        if (gameElements == null)
            yield break;
        foreach (var gameElement in gameElements)
        {
            var gameDetails = new GameDetails();
            var relativeUrl = gameElement.GetAttribute("href");
            var title = gameElement.QuerySelector(".title")?.TextContent;
            var platformElements = gameElement.QuerySelectorAll("ul.system-list > li.system:not(.more)");
            gameDetails.Url = new Uri(new Uri(url), relativeUrl).AbsoluteUri;
            gameDetails.Names.Add(title);
            gameDetails.Platforms.AddRange(platformElements.SelectMany(p => platformUtility.GetPlatforms(p.TextContent)));
            yield return gameDetails;
        }
    }

    private IEnumerable<GameDetails> GetPaginatedGames(string firstPageUrl, string gameElementSelector, GlobalProgressActionArgs progressArgs = null)
    {
        const string baseProgressString = "Downloading list of associated games...";
        if (progressArgs != null)
            progressArgs.Text = baseProgressString;

        var url = firstPageUrl;

        do
        {
            var response = downloader.DownloadString(url);
            var htmlDocument = new HtmlParser().Parse(response.ResponseContent);
            var pagination = GetPaginationInfo(url, htmlDocument);
            if (pagination != null)
            {
                if (progressArgs != null)
                {
                    progressArgs.IsIndeterminate = false;
                    progressArgs.ProgressMaxValue = pagination.TotalPages;
                    progressArgs.CurrentProgressValue = pagination.CurrentPage;
                    progressArgs.Text = $"{baseProgressString} page {pagination.CurrentPage}/{pagination.TotalPages}";
                }
            }
            foreach (var game in GetGamesFromDocument(url, htmlDocument, gameElementSelector))
                yield return game;

            url = pagination?.NextPageUrl;
        } while (url != null && progressArgs?.CancelToken.IsCancellationRequested != true);
    }

    private class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string NextPageUrl { get; set; }
    }
}
