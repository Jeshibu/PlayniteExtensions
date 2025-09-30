using Newtonsoft.Json;
using Playnite.SDK;
using RestSharp;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ViveportLibrary.Api;

public class ViveportApiClient : IViveportApiClient
{
    private const string BaseUrl = "https://www.viveport.com/";
    private static readonly CookieContainer cookies = new();
    private readonly RestClient restClient;
    private readonly ILogger logger = LogManager.GetLogger();

    public ViveportApiClient(RestClient restClient = null)
    {
        this.restClient = restClient ?? new RestClient(new RestClientOptions(BaseUrl) { Expect100Continue = false, CookieContainer = cookies });
    }

    private async Task<T> Execute<T>(RestRequest request, CancellationToken cancellationToken = default)
    {
        logger.Debug($"{request.Method} {request.Resource}");
        var response = await restClient.ExecuteAsync<T>(request, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            logger.Debug("Request cancelled");
            return default;
        }

        if (response == null)
        {
            logger.Debug("No response");
            return default;
        }
        logger.Debug($"{request.Resource} Response code {response.StatusCode}");
        logger.Trace($"{request.Resource} Content: {response.Content}");

        if (string.IsNullOrWhiteSpace(response.Content))
            return default;
        var output = JsonConvert.DeserializeObject<T>(response.Content);
        return output;
    }


    public async Task<CmsAppDetailsResponse> GetGameDetailsAsync(string appId, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest("api/cms/v4/products/a/all", Method.Post)
                            .AddQueryParameter("uKey", appId)
                            .AddJsonBody2(new Dictionary<string, object>
                            {
                                { "app_ids", new[]{ appId } },
                                { "locale", "en-US" },
                                { "show_coming_soon", true },
                                { "content_genus", "all" },
                                { "subscription_only", 1 },
                                { "include_unpublished", false }
                            });

        return await Execute<CmsAppDetailsResponse>(request, cancellationToken);
    }

    public async Task<GetCustomAttributeResponseRoot> GetAttributesAsync(CancellationToken cancellationToken = default)
    {
        var request = new RestRequest("graphql", Method.Post).AddJsonBody2(new Dictionary<string, object>
        {
            { "operationName", "GetCustomAttribute" },
            { "variables", new Dictionary<string,object>() },
            { "query", "query GetCustomAttribute {\n  customAttributeMetadata(\n    attributes: [{attribute_code: \"app_type\", entity_type: \"catalog_product\"}, {attribute_code: \"content_type\", entity_type: \"catalog_product\"}, {attribute_code: \"genre\", entity_type: \"catalog_product\"}, {attribute_code: \"headsets\", entity_type: \"catalog_product\"}, {attribute_code: \"media_type\", entity_type: \"catalog_product\"}, {attribute_code: \"play_area\", entity_type: \"catalog_product\"}, {attribute_code: \"input_methods\", entity_type: \"catalog_product\"}, {attribute_code: \"player_num\", entity_type: \"catalog_product\"}, {attribute_code: \"lang_supported\", entity_type: \"catalog_product\"}, {attribute_code: \"content_rating\", entity_type: \"catalog_product\"}]\n  ) {\n    items {\n      attribute_code\n      attribute_options {\n        admin_label\n        value\n        label\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n}\n" }
        });

        return await Execute<GetCustomAttributeResponseRoot>(request, cancellationToken);
    }
}

internal static class RestHelpers
{
    internal static RestRequest AddJsonBody2(this RestRequest request, object obj)
    {
        var body = JsonConvert.SerializeObject(obj);
        return request.AddBody(body);
    }
}
