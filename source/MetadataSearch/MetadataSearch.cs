using MetadataSearch.SearchItems;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using MetadataSearch.Settings;

namespace MetadataSearch;

public class MetadataSearch : GenericPlugin
{
    public MetadataSearch(IPlayniteAPI playniteApi) : base(playniteApi)
    {
        Properties = new GenericPluginProperties { HasSettings = true };
        Settings = new(this, PlayniteApi);
        Searches = GetSearchSupports();
    }

    public override Guid Id => new("bd6bdf7f-86d8-4fa9-b056-29c8753d475f");
    private MetadataSearchSettingsViewModel Settings { get; set; }
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
    public override UserControl GetSettingsView(bool firstRunView) => new MetadataSearchSettingsView();

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
        FilterProperty.FilterPreset => new DbObjFilterSearchContext<FilterPreset, FilterPresetSearchItem>(playniteApi, pn => pn.Database.FilterPresets, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Library => new DbObjFilterSearchContext<DatabaseObject, LibraryFilterSearchItem>(playniteApi, GetPlugins, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.AgeRating => new DbObjFilterSearchContext<AgeRating, AgeRatingFilterSearchItem>(playniteApi, pn => pn.Database.AgeRatings, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Category => new DbObjFilterSearchContext<Category, CategoryFilterSearchItem>(playniteApi, pn => pn.Database.Categories, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.CompletionStatus => new DbObjFilterSearchContext<CompletionStatus, CompletionStatusFilterSearchItem>(playniteApi, pn => pn.Database.CompletionStatuses, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Developer => new DbObjFilterSearchContext<Company, DeveloperFilterSearchItem>(playniteApi, pn => pn.Database.Companies, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Publisher => new DbObjFilterSearchContext<Company, PublisherFilterSearchItem>(playniteApi, pn => pn.Database.Companies, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Feature => new DbObjFilterSearchContext<GameFeature, FeatureFilterSearchItem>(playniteApi, pn => pn.Database.Features, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Genre => new DbObjFilterSearchContext<Genre, GenreFilterSearchItem>(playniteApi, pn => pn.Database.Genres, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Platform => new DbObjFilterSearchContext<Platform, PlatformFilterSearchItem>(playniteApi, pn => pn.Database.Platforms, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Region => new DbObjFilterSearchContext<Region, RegionFilterSearchItem>(playniteApi, pn => pn.Database.Regions, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Series => new DbObjFilterSearchContext<Series, SeriesFilterSearchItem>(playniteApi, pn => pn.Database.Series, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Source => new DbObjFilterSearchContext<GameSource, SourceFilterSearchItem>(playniteApi, pn => pn.Database.Sources, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Tag => new DbObjFilterSearchContext<Tag, TagFilterSearchItem>(playniteApi, pn => pn.Database.Tags, x => new(playniteApi.MainView, x, appendFilterIsPrimary)),
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