using PlayniteExtensions.Tests.Common;
using System.Linq;

namespace WikipediaCategories.Tests;

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
        Assert.Equal($"Wikipedia Category Importer {pluginVersion} (Playnite 10.47, https://github.com/Jeshibu/PlayniteExtensions)", _downloader.UserAgent);
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
    public void GetGameCategories()
    {
        var title = "Assassin's Creed II";
        _downloader.FilesByUrl.Add(_api.GetArticleUrl(title), "Resources/details-game-asscreed2.json");
        var article = _api.GetArticleCategories(title);
        Assert.NotEmpty(article.Categories);
    }

    [Fact]
    public void SearchCategories()
    {
        var query = "Video Games";
        _downloader.FilesByUrl.Add(_api.GetSearchUrl(query, WikipediaNamespace.Category), "Resources/search-categories.json");
        var searchResults = _api.Search(query, WikipediaNamespace.Category).ToList();
        Assert.NotEmpty(searchResults);
    }

    [Fact]
    public void GetCategoryMembers()
    {
        var pageName = "Category:Video_games_set_in_the_11th_century";
        _downloader.FilesByUrl.Add(_api.GetCategoryMembersUrl(pageName), "Resources/details-category-11th-century.json");
        var categoryMembers = _api.GetCategoryMembers(pageName);
        Assert.NotEmpty(categoryMembers);
    }
}
