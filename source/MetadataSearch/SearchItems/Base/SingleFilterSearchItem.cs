using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Linq.Expressions;

namespace MetadataSearch.SearchItems.Base;

public abstract class SingleFilterSearchItem<T> : MetadataFilterSearchItem<T> where T : DatabaseObject
{
    public SingleFilterSearchItem(IMainViewAPI mainViewAPI, T databaseObject, string type, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, type, appendFilterIsPrimary) { }

    protected static void Append(FilterPreset fp1, DatabaseObject dbObj, Expression<Func<FilterPresetSettings, IdItemFilterItemProperties>> selector) => Append(fp1.Settings, [dbObj.Id], selector);
}
