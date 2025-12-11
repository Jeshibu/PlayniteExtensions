using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using BigFishMetadata.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace BigFishMetadata;

public class BigFishSearchProvider(IWebDownloader downloader, BigFishMetadataSettings settings) : IGameSearchProvider<BigFishSearchResultGame>
{
    private readonly ILogger _logger = LogManager.GetLogger();
    private readonly Guid _bigFishLibraryId = Guid.Parse("37995df7-2ce2-4f7c-83a3-618138ae745d");
    private readonly BigFishGraphQLService _service = new(downloader);

    public GameDetails GetDetails(BigFishSearchResultGame searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        if (searchResult == null) return null;

        var response = downloader.DownloadString(searchResult.Url);
        _logger.Info($"Response {response.StatusCode} from {searchResult.Url}");

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;

        var output = new GameDetails { Url = searchResult.Url, Links = [new Link { Name = "Big Fish Games", Url = searchResult.Url }] };

        var doc = new HtmlParser().Parse(response.ResponseContent);
        var reviewFetchTask = GetCommunityScore(GetReviewsUrl(doc));

        output.Names.Add(doc.QuerySelector("[data-testid=ProductFullDetail-productName]")?.TextContent.HtmlDecode());
        output.Description = doc.QuerySelector(".override-productFullDetail-description-Q3J")?.InnerHtml.Trim();

        var additionalInfoHeaders = doc.QuerySelectorAll(".override-productFullDetail-details-Jgl > h3");
        foreach (var header in additionalInfoHeaders)
        {
            var headerText = header?.TextContent.HtmlDecode().ToLowerInvariant();
            var content = header?.NextElementSibling;
            if (header == null || content == null)
                continue;

            static List<string> GetLinkTexts(IElement htmlElement) => htmlElement.QuerySelectorAll("a").Select(x => x.TextContent.HtmlDecode()).ToList();

            switch (headerText)
            {
                case "genres":
                    output.Genres = GetLinkTexts(content);
                    foreach (var a in content.QuerySelectorAll("a[href]"))
                    {
                        var genreName = a.TextContent.HtmlDecode();
                        if(a.GetAttribute("href").Contains("/game-series/"))
                            output.Series.Add(genreName);
                        else
                            output.Genres.Add(genreName);
                    }
                    break;
                case "developer":
                    output.Developers = content.QuerySelectorAll("a").Select(x => x.TextContent.HtmlDecode()).ToList();
                    break;
                case "release date":
                    if (DateTime.TryParse(content.TextContent.Trim(), out var releaseDate))
                        output.ReleaseDate = new(releaseDate);
                    break;
                case "system requirements":
                    var match = Regex.Match("Hard Drive: (?<installsize>[0-9]+)", content.TextContent.HtmlDecode());
                    if (match.Success && ulong.TryParse(match.Groups["installsize"].Value, out ulong megaBytes))
                        output.InstallSize = megaBytes * 1024 * 1024;
                    break;
                default:
                    _logger.Info($"Unknown header: {header}");
                    break;
            }
        }

        var coverUrl = doc.QuerySelector("img.override-productFullDetail-featureImage-O2t")?.GetAttribute("src");
        if (!string.IsNullOrWhiteSpace(coverUrl))
            output.CoverOptions.Add(new BasicImage(coverUrl));

        output.BackgroundOptions = doc.QuerySelectorAll("img.productMedia-imageThumbnail-pPR").Select(x => new BasicImage(x.GetAttribute("src"))).ToList<IImageData>();

        reviewFetchTask.Wait();
        output.CommunityScore = reviewFetchTask.Result;

        return output;
    }

    private static string GetReviewsUrl(IHtmlDocument doc)
    {
        var gameId = doc.QuerySelector(".productFullDetail__root").GetAttribute("data-id");
        return GetReviewsUrl(gameId);
    }

    public static string GetReviewsUrl(string gameId) =>
        $"https://shop.bigfishgames.com/graphql?query=query+getReviews%28%24productId%3AInt%21%24page%3AInt%21%24amreviewDir%3AString%24amreviewSort%3AString%24stars%3AInt%24withImages%3ABoolean%24verifiedBuyer%3ABoolean%24isRecommended%3ABoolean%29%7Badvreview%28productId%3A%24productId+page%3A%24page+amreviewDir%3A%24amreviewDir+amreviewSort%3A%24amreviewSort+stars%3A%24stars+withImages%3A%24withImages+verifiedBuyer%3A%24verifiedBuyer+isRecommended%3A%24isRecommended%29%7BtotalRecords+ratingSummary+ratingSummaryValue+recomendedPercent+totalRecordsFiltered+detailedSummary%7Bone+two+three+four+five+__typename%7Ditems%7Breview_id+created_at+answer+verified_buyer+is_recommended+detail_id+title+detail+nickname+like_about+not_like_about+guest_email+plus_review+minus_review+rating_votes%7Bvote_id+option_id+rating_id+review_id+percent+value+rating_code+__typename%7Dimages%7Bfull_path+resized_path+__typename%7Dcomments%7Bid+review_id+status+message+nickname+email+created_at+updated_at+__typename%7D__typename%7D__typename%7D%7D&operationName=getReviews&variables=%7B%22page%22%3A1%2C%22productId%22%3A{gameId}%7D";

    private async Task<int?> GetCommunityScore(string url)
    {
        var response = await downloader.DownloadStringAsync(url);
        _logger.Info($"Response {response.StatusCode} from {url}");
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            return null;

        var reviewData = JsonConvert.DeserializeObject<ResponseRoot<ReviewJsonData>>(response.ResponseContent)?.data?.advreview;

        if (reviewData == null)
            return null;

        return settings.CommunityScoreType switch
        {
            CommunityScoreType.StarRating => (int)Math.Round(reviewData.ratingSummaryValue * 20),
            CommunityScoreType.PercentageRecommended => reviewData.recomendedPercent,
            _ => null,
        };
    }

    public IEnumerable<BigFishSearchResultGame> Search(string query, CancellationToken cancellationToken = default)
    {
        var searchUrl = $"https://www.bigfishgames.com/search.html?query={HttpUtility.UrlEncode(query)}&page=1&platform[filter]=Windows,150&language[filter]={settings.SelectedLanguage},{(int)settings.SelectedLanguage}";
        var response = downloader.DownloadString(searchUrl);
        _logger.Info($"Response {response.StatusCode} from {searchUrl}");

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            yield break;

        var doc = new HtmlParser().Parse(response.ResponseContent);
        foreach (var gameElement in doc.QuerySelectorAll("[data-cy=GalleryItem-root]"))
        {
            var item = new BigFishSearchResultGame();
            var titleElement = gameElement.QuerySelector("a[data-cy=GalleryItem-name]");
            item.Name = titleElement.TextContent.HtmlDecode();
            item.Url = titleElement.GetAttribute("href").GetAbsoluteUrl(searchUrl);
            item.CoverUrl = gameElement.QuerySelector("a[aria-label] img[loading=lazy]")?.GetAttribute("src");

            var attributes = gameElement.QuerySelectorAll("div.product-attributes").Select(e => e.TextContent.HtmlDecode()).ToList();
            item.Platform = attributes.LastOrDefault()?.Split([" | "], StringSplitOptions.None).Select(a => a.Trim()).First();
            if (DateTime.TryParse(attributes.FirstOrDefault(), out var releaseDate))
                item.ReleaseDate = new(releaseDate);
            yield return item;
        }
    }

    public GenericItemOption<BigFishSearchResultGame> ToGenericItemOption(BigFishSearchResultGame item) =>
        new(item) { Name = item.Name, Description = item.Platform };

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        gameDetails = null;
        return false;
    }
}

public class BigFishSearchResultGame : IGameSearchResult
{
    public string Name { get; set; }

    public string Platform { get; set; }

    public string CoverUrl { get; set; }

    public string Url { get; set; }

    public ReleaseDate? ReleaseDate { get; set; }

    string IGameSearchResult.Title => Name;

    IEnumerable<string> IGameSearchResult.AlternateNames => [];

    IEnumerable<string> IGameSearchResult.Platforms => string.IsNullOrWhiteSpace(Platform) ? [] : [Platform];
}
