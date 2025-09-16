using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FilterSearch.SearchContexts;

public class DbObjFilterSearchContext<TDatabaseObject> : SearchContext
    where TDatabaseObject : DatabaseObject
{
    private readonly IEnumerable<TDatabaseObject> objects;
    private readonly Func<TDatabaseObject, SearchItem> toSearchItem;
    private static readonly PropertyInfo CacheProperty = typeof(SearchContext).GetProperty("AutoSearchCache", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public DbObjFilterSearchContext(IEnumerable<TDatabaseObject> objects, Func<TDatabaseObject, SearchItem> toSearchItem)
    {
        UseAutoSearch = true;
        this.objects = objects;
        this.toSearchItem = toSearchItem;
        
        if (objects is IItemCollection<TDatabaseObject> itemCollection)
            itemCollection.ItemCollectionChanged += OnItemCollectionChanged;
    }

    private void OnItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<TDatabaseObject> e)
    {
        CacheProperty.SetValue(this, null);
    }

    public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args) => objects.Select(toSearchItem);
}
