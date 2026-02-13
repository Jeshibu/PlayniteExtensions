using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using GOGMetadata.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace GOGMetadata;

public class GogApiClient(IWebDownloader downloader, GOGMetadataSettings settings, IPlatformUtility platformUtility) : IGameSearchProvider<GogSearchResponse.Product>
{
    private readonly ILogger logger = LogManager.GetLogger();

    public StorePageResult.ProductDetails GetGameStoreData(GogSearchResponse.Product product)
    {
        if (product?.slug == null)
            return null;

        string url = $"https://www.gog.com/en/game/{product.slug}";
        string[] data;

        try
        {
            data = downloader.DownloadString(url).ResponseContent.Split('\n');
        }
        catch (WebException)
        {
            return null;
        }

        var dataStarted = false;
        var stringData = string.Empty;
        foreach (var line in data)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("window.productcardData"))
            {
                dataStarted = true;
                stringData = trimmed.Substring(25).TrimEnd(';');
                continue;
            }

            if (line.TrimStart().StartsWith("window.activeFeatures"))
            {
                var desData = Serialization.FromJson<StorePageResult>(stringData.TrimEnd(';'));

                return desData.cardProduct;
            }

            if (dataStarted)
                stringData += trimmed;
        }

        logger.Warn("Failed to get store data from page, no data found. " + url);
        return null;
    }

    public ProductApiDetail GetGameDetails(string id)
    {
        try
        {
            var response = downloader.DownloadString($"https://api.gog.com/products/{id}?expand=description&locale={settings.Locale}");
            return Serialization.FromJson<ProductApiDetail>(response.ResponseContent);
        }
        catch (WebException exc)
        {
            logger.Warn(exc, "Failed to download GOG game details for " + id);
            return null;
        }
    }

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        gameDetails = null;
        return false;
    }

    public GameDetails GetDetails(GogSearchResponse.Product searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        var storeGame = GetGameStoreData(searchResult);
        var gogDetails = GetGameDetails(searchResult.id);

        var output = new GameDetails
        {
            Names = AsList(searchResult.title),
            Developers = searchResult.developers?.SelectMany(StringExtensions.SplitCompanies).ToList(),
            Publishers = searchResult.publishers?.SelectMany(StringExtensions.SplitCompanies).ToList(),
            Features = AsList(searchResult.features),
            Genres = AsList(searchResult.genres),
            Tags = AsList(searchResult.tags),
            ReleaseDate = searchResult.ReleaseDate,
            CoverOptions = [new BasicImage(settings.UseVerticalCovers ? searchResult.coverVertical : searchResult.coverHorizontal)],
            Platforms = searchResult.operatingSystems?.SelectMany(platformUtility.GetPlatforms).ToList() ?? [],
            CommunityScore = searchResult.reviewsRating * 2,
            Links = [new("GOG Store Page", $"https://www.gog.com/{settings.Locale}/game/{searchResult.slug}")],
        };

        if (gogDetails != null)
        {
            output.Description = RemoveDescriptionPromos(gogDetails.description?.full)?.Trim();

            if (!string.IsNullOrEmpty(gogDetails.links.forum))
                output.Links.Add(new Link("GOG Forum", gogDetails.links.forum));

            if (!string.IsNullOrEmpty(gogDetails.images?.icon))
                output.IconOptions.Add(new BasicImage("http:" + gogDetails.images.icon));
        }

        if (storeGame != null)
        {
            output.InstallSize = (ulong)storeGame.size * 1024 * 1024;

            if (output.ReleaseDate == null && storeGame.globalReleaseDate != null)
                output.ReleaseDate = new ReleaseDate(storeGame.globalReleaseDate.Value);
        }

        var storeBackground = storeGame?.galaxyBackgroundImage ?? storeGame?.backgroundImage;
        foreach (var backgroundSource in settings.BackgroundTypePriority)
        {
            var addOptions = backgroundSource switch
            {
                BackgroundType.Screenshot => searchResult.screenshots?.Select(ScreenshotUrlToImageData).ToList() ?? [],
                BackgroundType.Background when gogDetails?.images?.background != null => [new BasicImage("http:" + gogDetails.images.background) { Description = "Background" }],
                BackgroundType.StoreBackground when storeBackground != null => [new BasicImage(storeBackground.Replace(".jpg", "_bg_crop_1920x655.jpg")) { Description = "Store background" }],
                _ => [],
            };
            output.BackgroundOptions.AddRange(addOptions);
        }

        return output;
    }

    private static List<string> AsList(string single)
    {
        if (string.IsNullOrEmpty(single))
            return [];

        return [single];
    }

    private static List<string> AsList(IEnumerable<SluggedName> sluggedNames)
    {
        if (sluggedNames == null)
            return [];

        return sluggedNames.Select(x => x.name).ToList();
    }

    private static IImageData ScreenshotUrlToImageData(string url)
    {
        return new BasicImage(url.Replace("_{formatter}", ""))
        {
            Description = "Screenshot",
            ThumbnailUrl = url.Replace("{formatter}", "product_card_v2_thumbnail_271")
        };
    }

    public IEnumerable<GogSearchResponse.Product> Search(string query, CancellationToken cancellationToken = default)
    {
        var url = $"https://catalog.gog.com/v1/catalog?limit=20&locale={settings.Locale}&order=desc:score&page=1&productType=in:game,pack&query=like:{WebUtility.UrlEncode(query)}";

        try
        {
            var response = downloader.DownloadString(url);
            return Serialization.FromJson<GogSearchResponse>(response.ResponseContent)?.products;
        }
        catch (WebException exc)
        {
            logger.Warn(exc, "Failed to get GOG store search data for " + query);
            return null;
        }
    }

    public GenericItemOption<GogSearchResponse.Product> ToGenericItemOption(GogSearchResponse.Product item)
    {
        var output = new GenericItemOption<GogSearchResponse.Product>(item) { Name = item.title, Description = item.releaseDate ?? "" };
        if (item.operatingSystems?.Any() == true)
            output.Description += " | " + string.Join(", ", item.operatingSystems);

        return output;
    }

    private static string RemoveDescriptionPromos(string originalDescription)
    {
        if (string.IsNullOrEmpty(originalDescription))
            return originalDescription;

        // Get opening element in description. Promos are always at the start of description.
        // It has been seen that descriptions start with <a> or <div> elements
        var parser = new HtmlParser();
        var document = parser.Parse(originalDescription);
        var firstChild = document.Body.FirstChild;
        if (firstChild is not { NodeType: NodeType.Element } || !firstChild.HasChildNodes)
            return originalDescription;

        // It's possible to check if a description has a promo if the first element contains
        // a child img element with a src that points to know promo image url patterns
        var htmlElement = firstChild as IHtmlElement;
        var promoUrlsRegex = @"https:\/\/items.gog.com\/(promobanners|autumn|fall|summer|winter)\/";
        var containsPromoImage = htmlElement.QuerySelectorAll("img")
                                            .Any(img => img.HasAttribute("src") && Regex.IsMatch(img.GetAttribute("src"), promoUrlsRegex, RegexOptions.IgnoreCase));
        if (!containsPromoImage)
        {
            return originalDescription;
        }

        // Remove all following <hr> and <br> elements that GOG adds after a promo
        var nextSibling = firstChild.NextSibling;
        while (nextSibling is IHtmlHrElement or IHtmlBreakRowElement)
        {
            document.Body.RemoveChild(nextSibling);
            nextSibling = firstChild.NextSibling;
        }

        // Remove initial opening element and return description without promo
        document.Body.RemoveChild(firstChild);
        return document.Body.InnerHtml;
    }
}
