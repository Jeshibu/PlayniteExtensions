using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace FilterSearch.SearchItems.Base;

public abstract class MetadataFilterSearchItem<T> : SearchItem where T : DatabaseObject
{
    private IMainViewAPI MainView { get; }
    protected T DatabaseObject { get; }

    protected MetadataFilterSearchItem(IMainViewAPI mainViewApi, T databaseObject, string type, bool appendFilterIsPrimary = true) : base(databaseObject.Name, null)
    {
        MainView = mainViewApi;
        DatabaseObject = databaseObject;
        Description = type;

        var appendAction = new SearchItemAction("Append to filter", AppendToCurrentFilter);
        var exclusiveAction = new SearchItemAction("Filter exclusively", ReplaceFilter);

        PrimaryAction = appendFilterIsPrimary ? appendAction : exclusiveAction;
        SecondaryAction = appendFilterIsPrimary ? exclusiveAction : appendAction;
    }

    private void AppendToCurrentFilter()
    {
        var fp = new FilterPreset
        {
            Settings = MainView.GetCurrentFilterSettings(),
            GroupingOrder = MainView.Grouping,
            SortingOrder = MainView.SortOrder,
            SortingOrderDirection = MainView.SortOrderDirection
        };
        ApplyFilterImpl(fp);
        MainView.ApplyFilterPreset(fp);
    }

    private void ReplaceFilter()
    {
        if (DatabaseObject is FilterPreset x)
        {
            MainView.ApplyFilterPreset(x);
        }
        else
        {
            var fp = new FilterPreset { Settings = new() };
            ApplyFilterImpl(fp);
            MainView.ApplyFilterPreset(fp);
        }
    }

    protected abstract void ApplyFilterImpl(FilterPreset fp);

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