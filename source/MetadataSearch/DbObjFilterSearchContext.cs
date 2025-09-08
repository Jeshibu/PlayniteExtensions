using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MetadataSearch;

public class DbObjFilterSearchContext<TDatabaseObject, TSearchItem> : SearchContext
    where TDatabaseObject : DatabaseObject
    where TSearchItem : SearchItem
{
    private readonly IPlayniteAPI playniteApi;
    private readonly Func<IPlayniteAPI, IEnumerable<TDatabaseObject>> objectSelector;
    private readonly Func<TDatabaseObject, TSearchItem> toSearchItem;

    public DbObjFilterSearchContext(IPlayniteAPI playniteApi, Func<IPlayniteAPI, IEnumerable<TDatabaseObject>> objectSelector, Func<TDatabaseObject, TSearchItem> toSearchItem)
    {
        UseAutoSearch = true;
        this.playniteApi = playniteApi;
        this.objectSelector = objectSelector;
        this.toSearchItem = toSearchItem;
    }

    public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args) => objectSelector(playniteApi).Select(toSearchItem);
}
