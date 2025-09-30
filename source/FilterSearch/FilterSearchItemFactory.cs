using FilterSearch.SearchContexts;
using FilterSearch.SearchItems;
using FilterSearch.Settings;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace FilterSearch;

public class FilterSearchItemFactory(IPlayniteAPI playniteApi, FilterSearchSettings settings)
{
    private bool AppendFilterIsPrimary => settings.PrimaryAction == FilterActionType.Append;

    public List<SearchSupport> GetSearchSupports()
    {
        var results = new List<SearchSupport>();
        foreach (var s in settings.SearchProperties)
            if (s.EnableSearchContext)
                results.Add(GetSearchSupport(s.Property));

        if (settings.AddSortingSearchContext)
            results.Add(new("sort", "Sort games", new SortingSearchContext(playniteApi.MainView) { Label = "Sort games" }));

        if (settings.AddGroupingSearchContext)
            results.Add(new("group", "Group games", new GroupingSearchContext(playniteApi.MainView) { Label = "Group games" }));

        return results;
    }

    public IEnumerable<SearchItem> GetSearchGlobalCommands()
    {
        var result = new List<SearchItem>();

        foreach (var s in settings.SearchProperties)
        {
            if (!s.AddItemsToGlobalSearch)
                continue;

            var sc = GetSearchContext(s.Property);
            result.AddRange(sc.GetSearchResults(new()));
        }

        if (settings.GlobalInstallStatusItems)
        {
            result.Add(new InstallFilterSearchItem("Installed", playniteApi.MainView, true, false));
            result.Add(new InstallFilterSearchItem("Uninstalled", playniteApi.MainView, false, true));
            result.Add(new InstallFilterSearchItem("Clear installation status filter", playniteApi.MainView, false, false));
        }

        if (settings.GlobalFavoriteItem)
            result.Add(new ToggleFilterSearchItem("Favorite", playniteApi.MainView, fp => fp.Settings.Favorite = !fp.Settings.Favorite));

        if (settings.GlobalHiddenItem)
            result.Add(new ToggleFilterSearchItem("Hidden", playniteApi.MainView, fp => fp.Settings.Hidden = !fp.Settings.Hidden));

        if (settings.GlobalMatchAllItem)
            result.Add(new ToggleFilterSearchItem("Match all filters", playniteApi.MainView, fp => fp.Settings.UseAndFilteringStyle = !fp.Settings.UseAndFilteringStyle));

        if (settings.GlobalClearFilterItem)
            result.Add(new ClearFilterSearchItem(playniteApi.MainView));

        return result;
    }

    public SearchContext GetSearchContext(FilterProperty prop) => GetSearchContext(prop, playniteApi, AppendFilterIsPrimary);

    private SearchSupport GetSearchSupport(FilterProperty prop)
    {
        string description = GetSearchContextDescription(prop);
        var sc = GetSearchContext(prop);
        sc.Label = description;
        return new(GetDefaultKeyword(prop), description, sc);
    }

    private static SearchContext GetSearchContext(FilterProperty prop, IPlayniteAPI playniteApi, bool appendFilterIsPrimary) => prop switch
    {
        FilterProperty.FilterPreset => new DbObjFilterSearchContext<FilterPreset>(playniteApi.Database.FilterPresets, x => new FilterPresetSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Library => new LibraryFilterSearchContext(playniteApi, appendFilterIsPrimary),
        FilterProperty.AgeRating => new DbObjFilterSearchContext<AgeRating>(playniteApi.Database.AgeRatings, x => new AgeRatingFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Category => new DbObjFilterSearchContext<Category>(playniteApi.Database.Categories, x => new CategoryFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.CompletionStatus => new DbObjFilterSearchContext<CompletionStatus>(playniteApi.Database.CompletionStatuses, x => new CompletionStatusFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Developer => new DbObjFilterSearchContext<Company>(playniteApi.Database.Companies, x => new DeveloperFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Publisher => new DbObjFilterSearchContext<Company>(playniteApi.Database.Companies, x => new PublisherFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Feature => new DbObjFilterSearchContext<GameFeature>(playniteApi.Database.Features, x => new FeatureFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Genre => new DbObjFilterSearchContext<Genre>(playniteApi.Database.Genres, x => new GenreFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Platform => new DbObjFilterSearchContext<Platform>(playniteApi.Database.Platforms, x => new PlatformFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Region => new DbObjFilterSearchContext<Region>(playniteApi.Database.Regions, x => new RegionFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Series => new DbObjFilterSearchContext<Series>(playniteApi.Database.Series, x => new SeriesFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Source => new DbObjFilterSearchContext<GameSource>(playniteApi.Database.Sources, x => new SourceFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Tag => new DbObjFilterSearchContext<Tag>(playniteApi.Database.Tags, x => new TagFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
    };

    private static string GetSearchContextDescription(FilterProperty prop)
    {
        var name = prop switch
        {
            FilterProperty.FilterPreset => "Filter presets",
            FilterProperty.AgeRating => "age rating",
            FilterProperty.CompletionStatus => "completion status",
            _ => prop.ToString().ToLowerInvariant()
        };

        if (prop == FilterProperty.FilterPreset)
            return name;

        return $"Filter by {name}";
    }

    private static string GetDefaultKeyword(FilterProperty prop) => prop switch
    {
        FilterProperty.FilterPreset => "fp",
        FilterProperty.CompletionStatus => "completion",
        FilterProperty.Library => "lib",
        _ => prop.ToString().ToLowerInvariant()
    };
}
