using MetadataSearch.SearchItems.Base;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace MetadataSearch.SearchItems;
public sealed class TagFilterSearchItem : SingleFilterSearchItem<Tag>
{
    public TagFilterSearchItem(IMainViewAPI mainViewAPI, Tag databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Tag", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Tag);
}

public sealed class GenreFilterSearchItem : SingleFilterSearchItem<Genre>
{
    public GenreFilterSearchItem(IMainViewAPI mainViewAPI, Genre databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Genre", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Genre);
}

public sealed class AgeRatingFilterSearchItem : SingleFilterSearchItem<AgeRating>
{
    public AgeRatingFilterSearchItem(IMainViewAPI mainViewAPI, AgeRating databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Age rating", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.AgeRating);
}

public sealed class CategoryFilterSearchItem : SingleFilterSearchItem<Category>
{
    public CategoryFilterSearchItem(IMainViewAPI mainViewAPI, Category databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Category", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Category);
}

public sealed class CompletionStatusFilterSearchItem : SingleFilterSearchItem<CompletionStatus>
{
    public CompletionStatusFilterSearchItem(IMainViewAPI mainViewAPI, CompletionStatus databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Completion status", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.CompletionStatuses);
}

public sealed class DeveloperFilterSearchItem : SingleFilterSearchItem<Company>
{
    public DeveloperFilterSearchItem(IMainViewAPI mainViewAPI, Company databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Developer", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Developer);
}

public sealed class FeatureFilterSearchItem : SingleFilterSearchItem<GameFeature>
{
    public FeatureFilterSearchItem(IMainViewAPI mainViewAPI, GameFeature databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Feature", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Feature);
}

public sealed class PlatformFilterSearchItem : SingleFilterSearchItem<Platform>
{
    public PlatformFilterSearchItem(IMainViewAPI mainViewAPI, Platform databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Platform", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Platform);
}

public sealed class PublisherFilterSearchItem : SingleFilterSearchItem<Company>
{
    public PublisherFilterSearchItem(IMainViewAPI mainViewAPI, Company databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Publisher", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Publisher);
}

public sealed class RegionFilterSearchItem : SingleFilterSearchItem<Region>
{
    public RegionFilterSearchItem(IMainViewAPI mainViewAPI, Region databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Region", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Region);
}

public sealed class SeriesFilterSearchItem : SingleFilterSearchItem<Series>
{
    public SeriesFilterSearchItem(IMainViewAPI mainViewAPI, Series databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Series", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Series);
}

public sealed class SourceFilterSearchItem : SingleFilterSearchItem<GameSource>
{
    public SourceFilterSearchItem(IMainViewAPI mainViewAPI, GameSource databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Source", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Source);
}

public sealed class LibraryFilterSearchItem : SingleFilterSearchItem<DatabaseObject>
{
    public LibraryFilterSearchItem(IMainViewAPI mainViewAPI, DatabaseObject databaseObject, bool appendFilterIsPrimary = true)
        : base(mainViewAPI, databaseObject, "Library", appendFilterIsPrimary) { }

    protected override void ApplyFilterImpl(FilterPreset fp) => Append(fp, DatabaseObject, fs => fs.Library);
}
