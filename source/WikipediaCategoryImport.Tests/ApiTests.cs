using PlayniteExtensions.Tests.Common;

namespace WikipediaCategoryImport.Tests;

public class ApiTests
{
    private readonly FakeWebDownloader _downloader;
    private readonly WikipediaApi _api;

    public ApiTests()
    {
        _downloader = new();
        _api = new(_downloader, new(10, 47), "en");
    }

    [Fact]
    public void UserAgentContainsVersions()
    {
        var pluginVersion = _api.GetType().Assembly.GetName().Version;
        Assert.Equal($"Wikipedia Category Importer {pluginVersion} (Playnite 10.47)", _downloader.UserAgent);
    }

    [Fact]
    public void SearchAssCreed()
    {
        var query = "assassin's creed";
        _downloader.FilesByUrl.Add(_api.GetSearchUrl(query, WikipediaNamespace.Article), "Resources/search-game-asscreed.json");
        var searchResults = _api.Search(query, WikipediaNamespace.Article).ToList();
        Assert.NotEmpty(searchResults);
    }

    [Fact]
    public void GetCategories()
    {
        var title = "Assassin's Creed II";
        _downloader.FilesByUrl.Add(_api.GetArticleUrl(title), "Resources/details-game-asscreed2.json");
        var categories = _api.GetCategories(title);
        Assert.NotEmpty(categories);
        Assert.All(categories, c => Assert.DoesNotContain("Category:", c));
    }
}
