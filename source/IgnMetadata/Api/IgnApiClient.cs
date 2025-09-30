using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteExtensions.Common;

namespace IgnMetadata.Api;

public class IgnApiClient(IWebDownloader downloader)
{
    private readonly ILogger logger = LogManager.GetLogger();

    public ICollection<IgnGame> Search(string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return [];

        var variables = new { term = searchString, count = 20, objectType = "Game" };
        var data = Call<IgnSearchResultData>("SearchObjectsByName", variables, "e1c2e012a21b4a98aaa618ef1b43eb0cafe9136303274a34f5d9ea4f2446e884");

        return data?.SearchObjectsByName?.Objects;
    }

    public IgnGame Get(string slug, string region)
    {
        var variables = new { slug, objectType = "Game", region, state = "Published" };
        var data = Call<IgnGetGameResultData>("ObjectSelectByTypeAndSlug", variables, "b9c48f45a7390ecd157229419dc9a2acb48de90c0f255b667076befb38338de6");

        return data?.ObjectSelectByTypeAndSlug;
    }

    public IEnumerable<string> GetImages(string slug)
    {
        var variables = new { slug, objectType = "Game", count = 10 };
        var data = Call<IgnGetImagesResultData>("ObjectImageGallery", variables, "06204b0f0871f8382e3adab7d1c59399e6c17ac94bff575c20a12ebf9d880b86");

        return data?.ImageGallery?.Images.Select(i => i.Url);
    }

    private T Call<T>(string operationName, object variables, string hash) where T: class
    {
        var extensions = new { persistedQuery = new { version = 1, sha256Hash = hash } };
        var variablesParameter = ToQueryStringParameter(variables);
        var extensionsParameter = ToQueryStringParameter(extensions);
        string url = $"https://mollusk.apis.ign.com/graphql?operationName={operationName}&variables={variablesParameter}&extensions={extensionsParameter}";

        void HeaderSetter(HttpRequestHeaders headers)
        {
            headers.Add("apollographql-client-name", "kraken");
            headers.Add("apollographql-client-version", "v0.67.0");
        }

        var response = downloader.DownloadString(url, referer: "https://www.ign.com/reviews/games", headerSetter: HeaderSetter, contentType: "application/json");
        if (string.IsNullOrWhiteSpace(response?.ResponseContent))
        {
            logger.Error($"Failed to get content from {url}");
            return null;
        }

        var root = JsonConvert.DeserializeObject<IgnResponseRoot<T>>(response.ResponseContent);
        if (root != null && root.Errors.Any())
        {
            foreach (var error in root.Errors)
            {
                logger.Error(error.Message);
            }
            return null;
        }

        return root?.Data;
    }

    private static string ToQueryStringParameter(object obj)
    {
        return Uri.EscapeDataString(JsonConvert.SerializeObject(obj, Formatting.None));
    }
}