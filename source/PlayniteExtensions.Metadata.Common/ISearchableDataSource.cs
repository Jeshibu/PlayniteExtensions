using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;

namespace PlayniteExtensions.Metadata.Common
{
    public interface ISearchableDataSource<TSearchResult>
    {
        IEnumerable<TSearchResult> Search(string query);
        GenericItemOption<TSearchResult> ToGenericItemOption(TSearchResult item);
    }

    public interface ISearchableDataSourceWithDetails<TSearchResult, TDetails> : ISearchableDataSource<TSearchResult>
    {
        TDetails GetDetails(TSearchResult searchResult, GlobalProgressActionArgs progressArgs = null);
    }

    public interface IGameSearchProvider<TSearchResult> : ISearchableDataSourceWithDetails<TSearchResult, GameDetails>
    {
        /// <summary>
        /// Try to get the details from a game based on some ID found in the game (generally the links)
        /// </summary>
        /// <param name="game"></param>
        /// <param name="gameDetails">The found game details, null if nothing was found</param>
        /// <returns></returns>
        bool TryGetDetails(Game game, out GameDetails gameDetails);
    }
}