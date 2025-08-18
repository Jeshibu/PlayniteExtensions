using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MetadataSearch.SearchItems;

public abstract class MetadataFilterSearchItem<T> : SearchItem where T : DatabaseObject
{
    protected IMainViewAPI MainView { get; }
    protected T DatabaseObject { get; }

    protected MetadataFilterSearchItem(IMainViewAPI mainViewAPI, T databaseObject, string type, bool appendFilterIsPrimary = true) : base(databaseObject.Name, null)
    {
        MainView = mainViewAPI;
        DatabaseObject = databaseObject;
        Description = type;

        PrimaryAction = new SearchItemAction("Append to filter", AppendToCurrentFilter);
        SecondaryAction = new SearchItemAction("Filter exclusively", ReplaceFilter);
    }

    void AppendToCurrentFilter()
    {
        var fs = MainView.GetCurrentFilterSettings();
        var fp = new FilterPreset() { Settings = fs };
        ApplyFilterImpl(fp);
        MainView.ApplyFilterPreset(fp);
    }

    void ReplaceFilter()
    {
        var fp = new FilterPreset() { Settings = new() };
        ApplyFilterImpl(fp);
        MainView.ApplyFilterPreset(fp);
    }

    protected abstract void ApplyFilterImpl(FilterPreset fp);

    protected void Append(IdItemFilterItemProperties fp1, IdItemFilterItemProperties fp2)
    {
        if (fp2?.Ids == null)
            return;

        if (fp1?.Ids == null)
            (fp1.Ids ??= new()).AddRange(fp2.Ids);
    }

    protected static void Append(FilterPresetSettings fs1, List<Guid> addIds, Expression<Func<FilterPresetSettings, IdItemFilterItemProperties>> selector, Func<FilterPresetSettings, IdItemFilterItemProperties> compiledSelector = null)
    {
        if (addIds == null || addIds.Count == 0)
            return;

        var prop = (PropertyInfo)((MemberExpression)selector.Body).Member;

        compiledSelector ??= selector.Compile();
        var x1 = compiledSelector(fs1);
        if (x1?.Ids == null || x1.Ids.Count == 0)
        {
            var x2 = new IdItemFilterItemProperties(addIds);
            prop.SetValue(fs1, x2);
            return;
        }

        HashSet<Guid> mergedIds = [.. x1.Ids, .. addIds];
        prop.SetValue(fs1, new IdItemFilterItemProperties(mergedIds.ToList()));
    }
}
