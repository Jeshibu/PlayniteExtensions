using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using WikipediaCategories.Models;

namespace WikipediaCategories.BulkImport;

public interface IWikipediaCategorySearchProvider : IBulkPropertyImportDataSource<WikipediaSearchResult>
{

}

public class WikipediaCategorySearchProvider(WikipediaApi api) : IWikipediaCategorySearchProvider
{
    public IEnumerable<WikipediaSearchResult> Search(string query, CancellationToken cancellationToken = default)
    {
        return api.Search(query, WikipediaNamespace.Category, cancellationToken);
    }

    public GenericItemOption<WikipediaSearchResult> ToGenericItemOption(WikipediaSearchResult item) => new(item) { Name = item.Name };

    IEnumerable<GameDetails> ISearchableDataSourceWithDetails<WikipediaSearchResult, IEnumerable<GameDetails>>.GetDetails(WikipediaSearchResult searchResult, GlobalProgressActionArgs progressArgs, Game searchGame)
    {
        throw new NotImplementedException();
    }

    private IEnumerable<GameDetails> GetDetailsRecursive(string rootCategoryName, int depth, CancellationToken cancellationToken)
    {
        var output = new List<GameDetails>();
        if (depth >= MaxDepth)
            return output;

        var categoryMembers = api.GetCategoryMembers(rootCategoryName, cancellationToken);
        foreach (var categoryMember in categoryMembers)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            switch ((WikipediaNamespace)categoryMember.ns)
            {
                case WikipediaNamespace.Article:
                    AddGame(categoryMember.title);
                    break;
                case WikipediaNamespace.Category:
                    AddCategory(categoryMember.title);
                    break;
            }
        }
        return output;

        void AddCategory(string categoryName)
        {
            output.AddRange(GetDetailsRecursive(categoryName, depth + 1, cancellationToken));
        }

        void AddGame(string pageName)
        {
            var match = TitleParenthesesRegex.Match(pageName);
            if (match.Success)
            {
                var parenContents = match.Groups["parenContents"].Value;
                if (parenContents.Contains("comic") || parenContents.Contains("soundtrack") || parenContents.Contains("film") || parenContents.Contains("novel"))
                    return;
            }

            var displayTitle = match.Success ? match.Groups["title"].Value : pageName;
            output.Add(new()
            {
                Names = [displayTitle],
                Url = WikipediaIdUtility.ToWikipediaUrl(api.WikipediaLocale, pageName),
            });
        }
    }

    public CategoryContents GetCategoryContents(string categoryName, CancellationToken cancellationToken)
    {
        var output = new CategoryContents();

        var categoryMembers = api.GetCategoryMembers(categoryName, cancellationToken);
        foreach (var categoryMember in categoryMembers)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            switch ((WikipediaNamespace)categoryMember.ns)
            {
                case WikipediaNamespace.Article:
                    output.Articles.Add(categoryMember.title);
                    break;
                case WikipediaNamespace.Category:
                    output.Subcategories.Add(categoryMember.title);
                    break;
            }
        }
        return output;
    }

    private static readonly Regex TitleParenthesesRegex = new(@"(?<title>.+) \((?<parenContents>.+)\)", RegexOptions.Compiled);
    private const int MaxDepth = 8;
}
