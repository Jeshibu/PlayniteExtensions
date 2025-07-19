using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace BigFishMetadata;

public class BigFishSearchProvider : IGameSearchProvider<BigFishSearchResultGame>
{
    private readonly IWebDownloader downloader;
    private readonly BigFishMetadataSettings settings;
    private readonly ILogger logger = LogManager.GetLogger();
    private readonly Guid BigFishLibraryId = Guid.Parse("37995df7-2ce2-4f7c-83a3-618138ae745d");

    public BigFishSearchProvider(IWebDownloader downloader, BigFishMetadataSettings settings)
    {
        this.downloader = downloader;
        this.settings = settings;
    }

    public GameDetails GetDetails(BigFishSearchResultGame searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        if (searchResult == null) return null;

        var response = downloader.DownloadString(searchResult.Url);
        logger.Info($"Response {response.StatusCode} from {searchResult.Url}");

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;

        var output = new GameDetails() { Url = searchResult.Url, Links = [new Link { Name = "Big Fish Games", Url = searchResult.Url }] };

        var doc = new HtmlParser().Parse(response.ResponseContent);
        var reviewFetchTask = GetCommunityScore(GetReviewsUrl(doc));

        output.Names.Add(doc.QuerySelector(".productFullDetail__productName")?.TextContent.Trim());
        output.Description = doc.QuerySelector(".productFullDetail__description")?.InnerHtml.Trim();
        output.InstallSize = doc.QuerySelector(".productFullDetail__requirementsGame")?.Children.Last().TextContent.ParseInstallSize();
        output.Genres = doc.QuerySelectorAll(".productFullDetail__genreLink").Select(g => g.TextContent.Trim()).ToList();
        var coverUrl = doc.QuerySelector(".productFullDetail__art > img")?.GetAttribute("src");
        if (!string.IsNullOrWhiteSpace(coverUrl))
            output.CoverOptions.Add(new BasicImage(coverUrl));

        output.BackgroundOptions = doc.QuerySelectorAll(".productFullDetail__screenshotNavItem > img").Select(x => new BasicImage(x.GetAttribute("src"))).ToList<IImageData>();

        reviewFetchTask.Wait();
        output.CommunityScore = reviewFetchTask.Result;

        return output;
    }

    private static string GetReviewsUrl(IHtmlDocument doc)
    {
        var translationId = doc.QuerySelector(".productFullDetail__root").GetAttribute("data-game-translation-id");
        var bvapiUrl = doc.QuerySelector(".productFullDetail__reviews > script").GetAttribute("src");
        return bvapiUrl.Replace("/static", "").TrimEnd("/bvapi.js") + $"/{translationId}/reviews.djs?format=embeddedhtml&suppressScroll=false";
    }

    private async Task<int?> GetCommunityScore(string url)
    {
        var response = await downloader.DownloadStringAsync(url);
        logger.Info($"Response {response.StatusCode} from {url}");
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;

        var reviewJsonString = response.ResponseContent
            ?.Split('\n')
            .FirstOrDefault(x => x.StartsWith("webAnalyticsConfig:"))
            ?.TrimStart("webAnalyticsConfig:")
            .TrimEnd('\r', ',');

        if (string.IsNullOrWhiteSpace(reviewJsonString))
            return null;

        var attributes = JsonConvert.DeserializeObject<ReviewJsonRoot>(reviewJsonString)?.JsonData?.Attributes;

        if (attributes == null)
            return null;

        return settings.CommunityScoreType switch
        {
            CommunityScoreType.StarRating => (int)Math.Round(attributes.AvgRating * 20),
            CommunityScoreType.PercentageRecommended => attributes.PercentRecommend,
            _ => null,
        };
    }

    private class ReviewJsonRoot
    {
        public ReviewJsonData JsonData { get; set; }
    }

    private class ReviewJsonData
    {
        public ReviewJsonAttributes Attributes { get; set; }
    }

    private class ReviewJsonAttributes
    {
        public int NumReviews { get; set; }
        public double AvgRating { get; set; }
        public int NumRatingsOnlyReviews { get; set; }
        public int PercentRecommend { get; set; }
    }

    public IEnumerable<BigFishSearchResultGame> Search(string query, CancellationToken cancellationToken = default)
    {
        var searchUrl = $"https://www.bigfishgames.com/us/en/games/search.html?language={(int)settings.SelectedLanguage}&search_query={HttpUtility.UrlEncode(query)}";
        var response = downloader.DownloadString(searchUrl);
        logger.Info($"Response {response.StatusCode} from {searchUrl}");

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            yield break;

        var doc = new HtmlParser().Parse(response.ResponseContent);
        foreach (var gameElement in doc.GetElementsByClassName("productcollection__item"))
        {
            var item = new BigFishSearchResultGame();
            var titleElement = gameElement.QuerySelector(".productcollection__item-title > a");
            item.Name = titleElement.TextContent.Trim();
            item.Url = titleElement.GetAttribute("href");
            item.CoverUrl = gameElement.QuerySelector(".productcollection__item-images img")?.GetAttribute("src");

            var attributes = gameElement.QuerySelector(".productcollection__item-attributes").TextContent.Trim();
            item.Platform = attributes.Split(new[] { " | " }, StringSplitOptions.None).Select(a => a.Trim()).First();
            yield return item;
        }
    }

    public GenericItemOption<BigFishSearchResultGame> ToGenericItemOption(BigFishSearchResultGame item) =>
        new GenericItemOption<BigFishSearchResultGame>(item) { Name = item.Name, Description = item.Platform };

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        gameDetails = null;
        if (game.PluginId == BigFishLibraryId)
        {
            var searchResult = Search(game.GameId).SingleOrDefault();
            gameDetails = GetDetails(searchResult);
            return gameDetails != null;
        }
        return false;
    }
}

public class BigFishSearchResultGame : IGameSearchResult
{
    public string Name { get; set; }

    public string Platform { get; set; }

    public string CoverUrl { get; set; }

    public string Url { get; set; }

    string IGameSearchResult.Title => Name;

    IEnumerable<string> IGameSearchResult.AlternateNames => Enumerable.Empty<string>();

    IEnumerable<string> IGameSearchResult.Platforms => string.IsNullOrWhiteSpace(Platform) ? Enumerable.Empty<string>() : new[] { Platform };

    ReleaseDate? IGameSearchResult.ReleaseDate => null;
}