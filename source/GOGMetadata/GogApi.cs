using AngleSharp.Parser.Html;
using GOGMetadata.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteExtensions.Common;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace GOGMetadata;

public class GogApi(IWebDownloader downloader)
{
    private readonly ILogger _logger = LogManager.GetLogger();

    public CatalogData GetCatalogData(string locale)
    {
        var response = downloader.DownloadString($"https://www.gog.com/{locale}/games");
        var doc = new HtmlParser().Parse(response.ResponseContent);
        var storeStateString = doc.QuerySelector("script#gogcom-store-state").TextContent;
        var parsedJson = JsonConvert.DeserializeObject<Dictionary<string, CatalogData>>(storeStateString);
        return parsedJson.FirstOrDefault(kvp => kvp.Key.StartsWith("catalog.gog/v1/catalog")).Value;
    }

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

        _logger.Warn("Failed to get store data from page, no data found. " + url);
        return null;
    }

    public ProductApiDetail GetGameDetails(string id, string locale)
    {
        try
        {
            var response = downloader.DownloadString($"https://api.gog.com/products/{id}?expand=description&locale={locale}");
            return Serialization.FromJson<ProductApiDetail>(response.ResponseContent);
        }
        catch (WebException exc)
        {
            _logger.Warn(exc, "Failed to download GOG game details for " + id);
            return null;
        }
    }

    public IEnumerable<GogSearchResponse.Product> Search(string query, string locale, CancellationToken cancellationToken = default)
    {
        var url = $"https://catalog.gog.com/v1/catalog?limit=20&locale={locale}&order=desc:score&page=1&productType=in:game,pack&query=like:{WebUtility.UrlEncode(query)}";

        try
        {
            var response = downloader.DownloadString(url);
            return Serialization.FromJson<GogSearchResponse>(response.ResponseContent)?.products;
        }
        catch (WebException exc)
        {
            _logger.Warn(exc, "Failed to get GOG store search data for " + query);
            return null;
        }
    }
}
