using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace WikipediaCategoryImport;

public class WikipediaGameSearchProvider(WikipediaApi api) : IGameSearchProvider<WikipediaGameSearchResult>
{
    private WikipediaIdUtility IdUtility { get; } = new();

    public IEnumerable<WikipediaGameSearchResult> Search(string query, CancellationToken cancellationToken = default)
    {
        var result = api.Search(query, WikipediaNamespace.Article, cancellationToken: cancellationToken).ToList();
        return result.Select(r => new WikipediaGameSearchResult(r.Name, r.Url));
    }

    public GenericItemOption<WikipediaGameSearchResult> ToGenericItemOption(WikipediaGameSearchResult item) => new(item) { Name = item.Name };

    public GameDetails GetDetails(WikipediaGameSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        var details = api.GetArticleCategories(searchResult.Name);
        var output = new GameDetails
        {
            Names = [details.Title, ..details.Redirects.Select(WikipediaGameSearchResult.StripParentheses)],
            Tags = details.Categories.Select(StripNameSpace).ToList(),
            Url = details.Url
        };
        return output;
    }

    private static string StripNameSpace(string pageName) => pageName?.Split([':'], 2).Last();

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        if (game?.Links == null)
        {
            gameDetails = null;
            return false;
        }

        foreach (var link in game.Links)
        {
            var id = IdUtility.GetIdFromUrl(link.Url);
            if (id.Database != ExternalDatabase.Wikipedia)
                continue;

            gameDetails = GetDetails(new(id.Id, link.Url), searchGame: game);
            if (gameDetails != null)
                return true;
        }

        gameDetails = null;
        return false;
    }
}
