using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Linq.Expressions;

namespace MetadataSearch.SearchItems.Base;

public abstract class SingleFilterSearchItem<T>(IMainViewAPI mainViewApi, T databaseObject, string type, bool appendFilterIsPrimary = true)
    : MetadataFilterSearchItem<T>(mainViewApi, databaseObject, type, appendFilterIsPrimary)
    where T : DatabaseObject
{
    protected static void Append(FilterPreset fp1, DatabaseObject dbObj, Expression<Func<FilterPresetSettings, IdItemFilterItemProperties>> selector) => Append(fp1.Settings, [dbObj.Id], selector);
}
