using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;

namespace MetadataSearch;

public class MetadataSearch : GenericPlugin
{
    public MetadataSearch(IPlayniteAPI playniteAPI) : base(playniteAPI)
    {

    }

    public override Guid Id { get; } = new("bd6bdf7f-86d8-4fa9-b056-29c8753d475f");

    public override IEnumerable<SearchItem> GetSearchGlobalCommands()
    {
        foreach (var filterPreset in PlayniteApi.Database.FilterPresets)
            yield return new FilterPresetSearchItem(PlayniteApi.MainView, filterPreset, "Filter preset");

        //foreach (var tag in PlayniteApi.Database.Tags)
        //    yield return new TagFilterSearchItem(PlayniteApi.MainView, tag, "Tag");
    }
}

public abstract class MetadataFilterSearchItem<T> : SearchItem where T : DatabaseObject
{
    protected IMainViewAPI MainView { get; }
    protected T DatabaseObject { get; }

    protected MetadataFilterSearchItem(IMainViewAPI mainViewAPI, T databaseObject, string type, bool appendFilterIsPrimary = true) : base($"{type}: {databaseObject.Name}", null)
    {
        MainView = mainViewAPI;
        DatabaseObject = databaseObject;
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
        if (fp2?.Ids != null)
            (fp1.Ids ??= new()).AddRange(fp2.Ids);
    }

    protected void Append(IdItemFilterItemProperties fp1, Guid id) => (fp1.Ids ??= new()).Add(id);

    protected void Append(EnumFilterItemProperties fp1, EnumFilterItemProperties fp2)
    {
        if (fp2?.Values != null)
            (fp1.Values ??= new()).AddRange(fp2.Values);
    }

    protected void Append(StringFilterItemProperties fp1, StringFilterItemProperties fp2)
    {
        if (fp2?.Values != null)
            (fp1.Values ??= new()).AddRange(fp2.Values);
    }
}

public class FilterPresetSearchItem : MetadataFilterSearchItem<FilterPreset>
{
    public FilterPresetSearchItem(IMainViewAPI mainViewAPI, FilterPreset databaseObject, string type, bool appendFilterIsPrimary = true) : base(mainViewAPI, databaseObject, type, appendFilterIsPrimary)
    {
    }

    protected override void ApplyFilterImpl(FilterPreset fp)
    {
        Append(fp.Settings.Added ??= new(), DatabaseObject.Settings.Added);
        Append(fp.Settings.AgeRating ??= new(), DatabaseObject.Settings.AgeRating);
        Append(fp.Settings.Category ??= new(), DatabaseObject.Settings.Category);
        Append(fp.Settings.CommunityScore ??= new(), DatabaseObject.Settings.CommunityScore);
        Append(fp.Settings.CompletionStatuses ??= new(), DatabaseObject.Settings.CompletionStatuses);
        Append(fp.Settings.CriticScore ??= new(), DatabaseObject.Settings.CriticScore);
        Append(fp.Settings.Developer ??= new(), DatabaseObject.Settings.Developer);
        fp.Settings.Favorite &= DatabaseObject.Settings.Favorite;
        Append(fp.Settings.Feature ??= new(), DatabaseObject.Settings.Feature);
        Append(fp.Settings.Genre ??= new(), DatabaseObject.Settings.Genre);
        fp.Settings.Hidden &= DatabaseObject.Settings.Hidden;
        Append(fp.Settings.InstallSize ??= new(), DatabaseObject.Settings.InstallSize);
        fp.Settings.IsInstalled &= DatabaseObject.Settings.IsInstalled;
        fp.Settings.IsUnInstalled &= DatabaseObject.Settings.IsUnInstalled;
        Append(fp.Settings.Modified ??= new(), DatabaseObject.Settings.Modified);
        if (string.IsNullOrEmpty(fp.Settings.Name))
            fp.Settings.Name = DatabaseObject.Settings.Name;
        Append(fp.Settings.Platform ??= new(), DatabaseObject.Settings.Platform);
        Append(fp.Settings.PlayTime ??= new(), DatabaseObject.Settings.PlayTime);
        Append(fp.Settings.Publisher ??= new(), DatabaseObject.Settings.Publisher);
        Append(fp.Settings.Publisher ??= new(), DatabaseObject.Settings.Publisher);
        Append(fp.Settings.RecentActivity ??= new(), DatabaseObject.Settings.RecentActivity);
        Append(fp.Settings.Region ??= new(), DatabaseObject.Settings.Region);
        Append(fp.Settings.ReleaseYear ??= new(), DatabaseObject.Settings.ReleaseYear);
        Append(fp.Settings.Series ??= new(), DatabaseObject.Settings.Series);
        Append(fp.Settings.Source ??= new(), DatabaseObject.Settings.Source);
        Append(fp.Settings.Tag ??= new(), DatabaseObject.Settings.Tag);
        fp.Settings.UseAndFilteringStyle &= DatabaseObject.Settings.UseAndFilteringStyle;
        Append(fp.Settings.UserScore ??= new(), DatabaseObject.Settings.UserScore);
        if (string.IsNullOrEmpty(fp.Settings.Version))
            fp.Settings.Version = DatabaseObject.Settings.Version;
    }
}

public class TagFilterSearchItem : MetadataFilterSearchItem<Tag>
{
    public TagFilterSearchItem(IMainViewAPI mainViewAPI, Tag databaseObject, string type, bool appendFilterIsPrimary = true) : base(mainViewAPI, databaseObject, type, appendFilterIsPrimary)
    {
    }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp.Settings.Tag ??= new(), DatabaseObject.Id);
}