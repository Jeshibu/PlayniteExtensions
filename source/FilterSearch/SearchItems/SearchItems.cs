using FilterSearch.SearchItems.Base;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace FilterSearch.SearchItems;

public sealed class TagFilterSearchItem(IMainViewAPI mainViewApi, Tag databaseObject, bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<Tag>(mainViewApi, databaseObject, "Tag", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Tag);
}

public sealed class GenreFilterSearchItem(
    IMainViewAPI mainViewApi,
    Genre databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<Genre>(mainViewApi, databaseObject, "Genre", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Genre);
}

public sealed class AgeRatingFilterSearchItem(
    IMainViewAPI mainViewApi,
    AgeRating databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<AgeRating>(mainViewApi, databaseObject, "Age rating", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.AgeRating);
}

public sealed class CategoryFilterSearchItem(
    IMainViewAPI mainViewApi,
    Category databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<Category>(mainViewApi, databaseObject, "Category", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Category);
}

public sealed class CompletionStatusFilterSearchItem(
    IMainViewAPI mainViewApi,
    CompletionStatus databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<CompletionStatus>(mainViewApi, databaseObject, "Completion status", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.CompletionStatuses);
}

public sealed class DeveloperFilterSearchItem(
    IMainViewAPI mainViewApi,
    Company databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<Company>(mainViewApi, databaseObject, "Developer", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Developer);
}

public sealed class FeatureFilterSearchItem(
    IMainViewAPI mainViewApi,
    GameFeature databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<GameFeature>(mainViewApi, databaseObject, "Feature", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Feature);
}

public sealed class PlatformFilterSearchItem(
    IMainViewAPI mainViewApi,
    Platform databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<Platform>(mainViewApi, databaseObject, "Platform", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Platform);
}

public sealed class PublisherFilterSearchItem(
    IMainViewAPI mainViewApi,
    Company databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<Company>(mainViewApi, databaseObject, "Publisher", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Publisher);
}

public sealed class RegionFilterSearchItem(
    IMainViewAPI mainViewApi,
    Region databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<Region>(mainViewApi, databaseObject, "Region", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Region);
}

public sealed class SeriesFilterSearchItem(
    IMainViewAPI mainViewApi,
    Series databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<Series>(mainViewApi, databaseObject, "Series", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Series);
}

public sealed class SourceFilterSearchItem(
    IMainViewAPI mainViewApi,
    GameSource databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<GameSource>(mainViewApi, databaseObject, "Source", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Source);
}

public sealed class LibraryFilterSearchItem(
    IMainViewAPI mainViewApi,
    DatabaseObject databaseObject,
    bool appendFilterIsPrimary = true)
    : SingleFilterSearchItem<DatabaseObject>(mainViewApi, databaseObject, "Library", appendFilterIsPrimary)
{
    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Library);
}
