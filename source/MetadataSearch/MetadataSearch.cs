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
    public MetadataSearch(IPlayniteAPI playniteAPI) : base(playniteAPI)
    {
        Properties = new GenericPluginProperties { HasSettings = true };
        Settings = new(this, PlayniteApi);
        Searches = GetSearchSupports();
    }

    public override Guid Id => new("bd6bdf7f-86d8-4fa9-b056-29c8753d475f");
    public MetadataSearchSettingsViewModel Settings { get; set; }
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

    private SearchSupport GetSearchSupport(FilterProperty prop, IPlayniteAPI playniteAPI)
    {
        string description = GetSearchContextDescription(prop);
        var sc = GetSearchContext(prop, playniteAPI, AppendFilterIsPrimary);
        sc.Label = description;
        return new(GetDefaultKeyword(prop), description, sc);
    }

    private static SearchContext GetSearchContext(FilterProperty prop, IPlayniteAPI playniteAPI, bool appendFilterIsPrimary) => prop switch
    {
        FilterProperty.FilterPreset => new DatabaseObjectFilterSearchContext<FilterPreset, FilterPresetSearchItem>(playniteAPI, pn => pn.Database.FilterPresets, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Library => new DatabaseObjectFilterSearchContext<DatabaseObject, LibraryFilterSearchItem>(playniteAPI, GetPlugins, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.AgeRating => new DatabaseObjectFilterSearchContext<AgeRating, AgeRatingFilterSearchItem>(playniteAPI, pn => pn.Database.AgeRatings, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Category => new DatabaseObjectFilterSearchContext<Category, CategoryFilterSearchItem>(playniteAPI, pn => pn.Database.Categories, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.CompletionStatus => new DatabaseObjectFilterSearchContext<CompletionStatus, CompletionStatusFilterSearchItem>(playniteAPI, pn => pn.Database.CompletionStatuses, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Developer => new DatabaseObjectFilterSearchContext<Company, DeveloperFilterSearchItem>(playniteAPI, pn => pn.Database.Companies, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Publisher => new DatabaseObjectFilterSearchContext<Company, PublisherFilterSearchItem>(playniteAPI, pn => pn.Database.Companies, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Feature => new DatabaseObjectFilterSearchContext<GameFeature, FeatureFilterSearchItem>(playniteAPI, pn => pn.Database.Features, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Genre => new DatabaseObjectFilterSearchContext<Genre, GenreFilterSearchItem>(playniteAPI, pn => pn.Database.Genres, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Platform => new DatabaseObjectFilterSearchContext<Platform, PlatformFilterSearchItem>(playniteAPI, pn => pn.Database.Platforms, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Region => new DatabaseObjectFilterSearchContext<Region, RegionFilterSearchItem>(playniteAPI, pn => pn.Database.Regions, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Series => new DatabaseObjectFilterSearchContext<Series, SeriesFilterSearchItem>(playniteAPI, pn => pn.Database.Series, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Source => new DatabaseObjectFilterSearchContext<GameSource, SourceFilterSearchItem>(playniteAPI, pn => pn.Database.Sources, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
        FilterProperty.Tag => new DatabaseObjectFilterSearchContext<Tag, TagFilterSearchItem>(playniteAPI, pn => pn.Database.Tags, x => new(playniteAPI.MainView, x, appendFilterIsPrimary)),
    };

    private static IEnumerable<DatabaseObject> GetPlugins(IPlayniteAPI playniteAPI)
    {
        return playniteAPI.Addons.Plugins.OfType<LibraryPlugin>().Select(p => new DatabaseObject { Id = p.Id, Name = p.Name });
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