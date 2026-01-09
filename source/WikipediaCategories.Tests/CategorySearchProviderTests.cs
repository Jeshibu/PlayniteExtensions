using PlayniteExtensions.Tests.Common;
using System.Threading;
using WikipediaCategories.BulkImport;

namespace WikipediaCategories.Tests;

public class CategorySearchProviderTests
{
    private readonly FakeWebDownloader _downloader;
    private readonly WikipediaApi _api;
    private readonly WikipediaCategorySearchProvider _categorySearchProvider;

    public CategorySearchProviderTests()
    {
        _downloader = new();
        _api = new(_downloader, new(10, 47), "en");
        _categorySearchProvider = new(_api);
    }

    [Fact]
    public void DoesNotRecurse()
    {
        var categoryName = "Category:Video games set in the 11th century";
        _downloader.FilesByUrl.Add(_api.GetCategoryMembersUrl(categoryName), "Resources/details-category-11th-century.json");
        //_downloader.FilesByUrl.Add(_api.GetCategoryMembersUrl("Category:Video games set in 11th-century Abbasid Caliphate"), "Resources/details-category-11th-century-subcat.json");
        var categoryContents = _categorySearchProvider.GetCategoryContents(categoryName, CancellationToken.None);
        Assert.Equal(13, categoryContents.Articles.Count);
        Assert.DoesNotContain("Sly Cooper: Thieves in Time", categoryContents.Articles);
        Assert.Single(categoryContents.Subcategories);
    }
}
