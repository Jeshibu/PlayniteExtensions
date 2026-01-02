using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace WikipediaCategoryImport;

public class WikipediaGameSearchProvider(IWebDownloader downloader) : IGameSearchProvider<WikipediaGameSearchResult>
{
    public IEnumerable<WikipediaGameSearchResult> Search(string query, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public GenericItemOption<WikipediaGameSearchResult> ToGenericItemOption(WikipediaGameSearchResult item)
    {
        throw new NotImplementedException();
    }

    public GameDetails GetDetails(WikipediaGameSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        throw new NotImplementedException();
    }

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
