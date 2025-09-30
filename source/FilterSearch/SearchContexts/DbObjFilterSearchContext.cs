using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FilterSearch.SearchContexts;

public sealed class DbObjFilterSearchContext<TDatabaseObject> : SearchContext
    where TDatabaseObject : DatabaseObject
{
    private readonly IItemCollection<TDatabaseObject> _itemCollection;
    private readonly Func<TDatabaseObject, SearchItem> _toSearchItem;
    private static readonly PropertyInfo CacheProperty = typeof(SearchContext).GetProperty("AutoSearchCache", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public DbObjFilterSearchContext(IItemCollection<TDatabaseObject> itemCollection, Func<TDatabaseObject, SearchItem> toSearchItem)
    {
        UseAutoSearch = true;
        _itemCollection = itemCollection;
        _toSearchItem = toSearchItem;

        itemCollection.ItemCollectionChanged += OnItemCollectionChanged;
    }

    private void OnItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<TDatabaseObject> e)
    {
        CacheProperty.SetValue(this, null);
    }

    public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args) => _itemCollection.Select(_toSearchItem);
}
