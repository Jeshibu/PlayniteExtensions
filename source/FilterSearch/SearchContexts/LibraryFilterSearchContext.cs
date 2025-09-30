using FilterSearch.SearchItems;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace FilterSearch.SearchContexts;

public sealed class LibraryFilterSearchContext : SearchContext
{
    private readonly IPlayniteAPI _playniteApi;
    private readonly bool _appendFilterIsPrimary;

    public LibraryFilterSearchContext(IPlayniteAPI playniteApi, bool appendFilterIsPrimary)
    {
        UseAutoSearch = true;
        _playniteApi = playniteApi;
        _appendFilterIsPrimary = appendFilterIsPrimary;
    }

    public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args)
    {
        return _playniteApi.Addons.Plugins.OfType<LibraryPlugin>().Select(p => new LibraryFilterSearchItem(_playniteApi.MainView, p, _appendFilterIsPrimary));
    }
}
