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
using WikipediaCategoryImport.Models;
using WikipediaCategoryImport.Models.API;

namespace WikipediaCategoryImport;

public class WikipediaApi
{
    private readonly string _baseUrl;
    private readonly IWebDownloader _downloader;
    private readonly string _wikipediaLocale;
    private readonly ILogger _logger = LogManager.GetLogger();

    public WikipediaApi(IWebDownloader downloader, Version playniteVersion, string wikipediaLocale = "en")
    {
        _downloader = downloader;
        _wikipediaLocale = wikipediaLocale;
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
            { "prop", "categories|redirects" },
            { "cllimit", "max" },
            { "rdlimit", "max" },
            { "redirects", null },
        }, continueParams);
    }

    public string GetCategoryMembersUrl(string pageName, Dictionary<string, string> continueParams = null)
    {
        return GetUrl(new()
        {
            { "action", "query" },
            { "list", "categorymembers" },
            { "cmtitle", pageName },
            { "cmlimit", "max" },
            { "cmprop", "title|type" },
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

    public ArticleDetails GetArticleCategories(string pageName, CancellationToken cancellationToken = default)
    {
        var output = new ArticleDetails();
        Dictionary<string, string> continueParams = null;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var article = GetArticleCategories(pageName, continueParams, cancellationToken);
            var page = article.query.pages.First().Value;
            output.Title ??= page.title;
            output.Url ??= WikipediaIdUtility.ToWikipediaUrl(_wikipediaLocale, page.title);
            output.Categories.AddRange(page.categories.Select(c => c.title));

            continueParams = article.@continue;

            if (continueParams == null)
                break;
        }

        return output;
    }

    public ICollection<CategoryMember> GetCategoryMembers(string pageName, CancellationToken cancellationToken = default)
    {
        var output = new List<CategoryMember>();
        Dictionary<string, string> continueParams = null;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var response = GetCategoryMembers(pageName, continueParams, cancellationToken);
            output.AddRange(response.query.categorymembers);

            continueParams = response.@continue;

            if (continueParams == null)
                break;
        }

        return output;
    }

    private WikipediaQueryResponse<PageQuery> GetArticleCategories(string pageName, Dictionary<string, string> continueParams, CancellationToken cancellationToken)
    {
        var url = GetArticleUrl(pageName, continueParams);

        var response = _downloader.DownloadString(url, cancellationToken: cancellationToken);
        var responseObj = JsonConvert.DeserializeObject<WikipediaQueryResponse<PageQuery>>(response.ResponseContent);
        return responseObj;
    }

    private WikipediaQueryResponse<CategoryMemberQueryResult> GetCategoryMembers(string pageName, Dictionary<string, string> continueParams, CancellationToken cancellationToken)
    {
        var url = GetCategoryMembersUrl(pageName, continueParams);

        var response = _downloader.DownloadString(url, cancellationToken: cancellationToken);
        var responseObj = JsonConvert.DeserializeObject<WikipediaQueryResponse<CategoryMemberQueryResult>>(response.ResponseContent);
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
