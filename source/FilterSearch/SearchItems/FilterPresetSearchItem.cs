using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FilterSearch.SearchItems.Base;

namespace FilterSearch.SearchItems;

public class FilterPresetSearchItem(IMainViewAPI mainViewApi, FilterPreset databaseObject, bool appendFilterIsPrimary)
    : MetadataFilterSearchItem<FilterPreset>(mainViewApi, databaseObject, "Filter preset", appendFilterIsPrimary)
{
    private static void Append(FilterPresetSettings fs1, EnumFilterItemProperties x2, Expression<Func<FilterPresetSettings, EnumFilterItemProperties>> selector, Func<FilterPresetSettings, EnumFilterItemProperties> compiledSelector = null)
    {
        if (x2?.Values == null || x2.Values.Count == 0)
            return;

        var prop = (PropertyInfo)((MemberExpression)selector.Body).Member;

        compiledSelector ??= selector.Compile();
        var x1 = compiledSelector(fs1);
        if (x1?.Values == null || x2.Values.Count == 0)
        {
            prop.SetValue(fs1, x2);
            return;
        }

        HashSet<int> mergedValues = [.. x1.Values, .. x2.Values];
        prop.SetValue(fs1, new EnumFilterItemProperties(mergedValues.ToList()));
    }

    private void Append(FilterPreset fp, Expression<Func<FilterPresetSettings, IdItemFilterItemProperties>> selector)
    {
        var compiledSelector = selector.Compile();
        var x2 = compiledSelector(DatabaseObject.Settings);
        Append(fp.Settings, x2?.Ids, selector, compiledSelector);
    }

    private void Append(FilterPreset fp, Expression<Func<FilterPresetSettings, EnumFilterItemProperties>> selector)
    {
        var compiledSelector = selector.Compile();
        var x2 = compiledSelector(DatabaseObject.Settings);
        Append(fp.Settings, x2, selector, compiledSelector);
    }

    private void AppendReleaseYear(FilterPreset fp)
    {
        if (DatabaseObject.Settings.ReleaseYear?.Values?.Any() != true)
            return;

        if (fp.Settings.ReleaseYear?.Values.Any() != true)
            fp.Settings.ReleaseYear = DatabaseObject.Settings.ReleaseYear;

        HashSet<string> merged = [.. fp.Settings.ReleaseYear.Values, .. DatabaseObject.Settings.ReleaseYear.Values];
        fp.Settings.ReleaseYear = new(merged.ToList());
    }

    protected override void ApplyFilterImpl(FilterPreset fp)
    {
        Append(fp, fs => fs.Added);
        Append(fp, fs => fs.AgeRating);
        Append(fp, fs => fs.Category);
        Append(fp, fs => fs.CommunityScore);
        Append(fp, fs => fs.CompletionStatuses);
        Append(fp, fs => fs.CriticScore);
        Append(fp, fs => fs.Developer);
        fp.Settings.Favorite |= DatabaseObject.Settings.Favorite;
        Append(fp, fs => fs.Feature);
        Append(fp, fs => fs.Genre);
        fp.Settings.Hidden &= DatabaseObject.Settings.Hidden;
        Append(fp, fs => fs.InstallSize);
        fp.Settings.IsInstalled |= DatabaseObject.Settings.IsInstalled;
        fp.Settings.IsUnInstalled |= DatabaseObject.Settings.IsUnInstalled;
        Append(fp, fs => fs.Library);
        Append(fp, fs => fs.Modified);
        if (string.IsNullOrEmpty(fp.Settings.Name))
            fp.Settings.Name = DatabaseObject.Settings.Name;
        Append(fp, fs => fs.Platform);
        Append(fp, fs => fs.PlayTime);
        Append(fp, fs => fs.Publisher);
        Append(fp, fs => fs.RecentActivity);
        Append(fp, fs => fs.Region);
        AppendReleaseYear(fp);
        Append(fp, fs => fs.Series);
        Append(fp, fs => fs.Source);
        Append(fp, fs => fs.Tag);
        fp.Settings.UseAndFilteringStyle |= DatabaseObject.Settings.UseAndFilteringStyle;
        Append(fp, fs => fs.UserScore);
        if (string.IsNullOrEmpty(fp.Settings.Version))
            fp.Settings.Version = DatabaseObject.Settings.Version;
    }
}
