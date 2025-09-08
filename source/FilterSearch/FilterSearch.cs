using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using FilterSearch.SearchItems;
using FilterSearch.Settings;

namespace FilterSearch;

public class FilterSearch : GenericPlugin
{
    public FilterSearch(IPlayniteAPI playniteApi) : base(playniteApi)
    {
        Properties = new GenericPluginProperties { HasSettings = true };
        Settings = new(this, PlayniteApi);
        Searches = GetSearchSupports();
    }

    public override Guid Id => new("bd6bdf7f-86d8-4fa9-b056-29c8753d475f");
    private FilterSearchSettingsViewModel Settings { get; set; }
    private bool AppendFilterIsPrimary => Settings.Settings.PrimaryAction == FilterActionType.Append;

    public override IEnumerable<SearchItem> GetSearchGlobalCommands()
    {
        var result = new List<SearchItem>();

        foreach (var s in Settings.Settings.SearchProperties)
        {
            if (!s.AddItemsToGlobalSearch)
                continue;
            
            var sc = GetSearchContext(s.Property, PlayniteApi, AppendFilterIsPrimary);
            result.AddRange(sc.GetSearchResults(new()));
        }

        return result;
    }

    public override ISettings GetSettings(bool firstRunSettings) => Settings;
    public override UserControl GetSettingsView(bool firstRunView) => new FilterSearchSettingsView();

    private List<SearchSupport> GetSearchSupports()
    {
        var results = new List<SearchSupport>();
        foreach (var s in Settings.Settings.SearchProperties)
        {
            if(s.EnableSearchContext)
                results.Add(GetSearchSupport(s.Property, PlayniteApi));
        }
        return results;
    }

    private SearchSupport GetSearchSupport(FilterProperty prop, IPlayniteAPI playniteApi)
    {
        string description = GetSearchContextDescription(prop);
        var sc = GetSearchContext(prop, playniteApi, AppendFilterIsPrimary);
        sc.Label = description;
        return new(GetDefaultKeyword(prop), description, sc);
    }

    private static SearchContext GetSearchContext(FilterProperty prop, IPlayniteAPI playniteApi, bool appendFilterIsPrimary) => prop switch
    {
        FilterProperty.FilterPreset => new DbObjFilterSearchContext<FilterPreset>(playniteApi.Database.FilterPresets, x => new FilterPresetSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Library => new DbObjFilterSearchContext<DatabaseObject>(GetPlugins(playniteApi), x => new LibraryFilterSearchItem(playniteApi.MainView, x, appendFilterIsPrimary)),
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

    private static IEnumerable<DatabaseObject> GetPlugins(IPlayniteAPI playniteApi)
    {
        return playniteApi.Addons.Plugins.OfType<LibraryPlugin>().Select(p => new DatabaseObject { Id = p.Id, Name = p.Name });
    }

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
        _ => prop.ToString().ToLowerInvariant()
    };
}

public enum FilterProperty
{
    FilterPreset,
    Library,
    AgeRating,
    Category,
    CompletionStatus,
    Developer,
    Publisher,
    Feature,
    Genre,
    Platform,
    Region,
    Series,
    Source,
    Tag
}