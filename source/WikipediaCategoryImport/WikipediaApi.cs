using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace WikipediaCategoryImport;

public class WikipediaApi
{
    private readonly string _baseUrl;
    private readonly IWebDownloader _downloader;
    private readonly ILogger _logger = LogManager.GetLogger();

    public WikipediaApi(IWebDownloader downloader, Version playniteVersion, string wikipediaLocale = "en")
    {
        _downloader = downloader;
        _baseUrl = $"https://{wikipediaLocale}.wikipedia.org/w/api.php?format=json";

        var pluginVersion = GetType().Assembly!.GetName().Version;
        _downloader.UserAgent = $"Wikipedia Category Importer {pluginVersion} (Playnite {playniteVersion})";
    }

    public string GetSearchUrl(string query, WikipediaNamespace ns)
    {
        return GetUrl(new()
        {
            { "action", "opensearch" },
            { "search", query },
            { "limit", "50" },
            { "namespace", ns.ToString() },
            //{ "redirects", "resolve" },
        });
    }

    public string GetArticleUrl(string pageName, Dictionary<string, string> continueParams = null)
    {
        return GetUrl(new()
        {
            { "action", "query" },
            { "titles", pageName },
            { "prop", "categories" },
            { "cllimit", "max" },
            { "redirects", null },
        }, continueParams);
    }

    public IEnumerable<WikipediaSearchResult> Search(string query, WikipediaNamespace ns, CancellationToken cancellationToken = default)
    {
        var url = GetSearchUrl(query, ns);
        var response = _downloader.DownloadString(url, cancellationToken: cancellationToken);
        var responseObj = JsonConvert.DeserializeObject<object[]>(response.ResponseContent);
        if (responseObj[1] is not JArray names
            || responseObj[2] is not JArray descriptions
            || responseObj[3] is not JArray urls)
        {
            _logger.Warn($"Failed to deserialize response from {url}: {response.ResponseContent}");
            yield break;
        }

        for (int i = 0; i < names.Count; i++)
        {
            yield return new()
            {
                Name = names[i].ToString(),
                Description = descriptions[i].ToString(),
                Url = urls[i].ToString(),
            };
        }
    }

    public ICollection<string> GetCategories(string pageName, CancellationToken cancellationToken = default)
    {
        var output = new List<string>();
        Dictionary<string, string> @continue = null;
        while (true)
        {
            var article = GetCategories(pageName, @continue, cancellationToken);
            foreach (var category in article.query.pages.First().Value.categories)
            {
                var trimmed = category.title.Split([':'], 2).Last();
                output.Add(trimmed);
            }

            if (article.@continue == null)
                break;

            @continue = article.@continue;
        }

        return output;
    }

    private WikipediaArticleResponse GetCategories(string pageName, Dictionary<string, string> continueParams = null, CancellationToken cancellationToken = default)
    {
        var url = GetArticleUrl(pageName, continueParams);

        var response = _downloader.DownloadString(url, cancellationToken: cancellationToken);
        var responseObj = JsonConvert.DeserializeObject<WikipediaArticleResponse>(response.ResponseContent);
        return responseObj;
    }

    private string GetUrl(Dictionary<string, string> parameters, Dictionary<string, string> continueParams = null)
    {
        StringBuilder sb = new(_baseUrl);

        void AddParameters(Dictionary<string, string> localParameters)
        {
            if (localParameters == null)
                return;

            foreach (var parameter in localParameters)
            {
                sb.Append('&').Append(parameter.Key);

                if (!string.IsNullOrEmpty(parameter.Value))
                    sb.Append('=').Append(Uri.EscapeDataString(parameter.Value));
            }
        }

        AddParameters(parameters);
        AddParameters(continueParams);

        return sb.ToString();
    }
}
