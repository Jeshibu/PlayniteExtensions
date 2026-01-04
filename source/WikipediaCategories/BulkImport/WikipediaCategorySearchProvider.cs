using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace WikipediaCategories.BulkImport;

public class WikipediaCategorySearchProvider(WikipediaApi api)
    : ISearchableDataSourceWithDetails<WikipediaSearchResult, IEnumerable<GameDetails>>
{
    public IEnumerable<WikipediaSearchResult> Search(string query, CancellationToken cancellationToken = default)
    {
        return api.Search(query, WikipediaNamespace.Category, cancellationToken);
    }

    public GenericItemOption<WikipediaSearchResult> ToGenericItemOption(WikipediaSearchResult item) => new(item) { Name = item.Name };

    public IEnumerable<GameDetails> GetDetails(WikipediaSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        return GetDetailsRecursive(searchResult.Name, 0, progressArgs?.CancelToken ?? CancellationToken.None);
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
                Url = WikipediaIdUtility.ToWikipediaUrl("en", pageName),
            });
        }
    }

    private static readonly Regex TitleParenthesesRegex = new(@"(?<title>.+) \((?<parenContents>.+)\)", RegexOptions.Compiled);
    private const int MaxDepth = 8;
}
