using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;

namespace MetadataSearch;

public class DatabaseObjectFilterSearchContext<TDatabaseObject, TSearchItem> : SearchContext
    where TDatabaseObject : DatabaseObject
    where TSearchItem : SearchItem
{
    private readonly IPlayniteAPI playniteAPI;
    private readonly Func<IPlayniteAPI, IEnumerable<TDatabaseObject>> objectSelector;
    private readonly Func<TDatabaseObject, TSearchItem> toSearchItem;

    public DatabaseObjectFilterSearchContext(IPlayniteAPI playniteAPI, Func<IPlayniteAPI, IEnumerable<TDatabaseObject>> objectSelector, Func<TDatabaseObject, TSearchItem> toSearchItem)
    {
        UseAutoSearch = true;
        this.playniteAPI = playniteAPI;
        this.objectSelector = objectSelector;
        this.toSearchItem = toSearchItem;
    }

    public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args)
    {
        foreach (var item in objectSelector(playniteAPI))
            yield return toSearchItem(item);
    }
}
