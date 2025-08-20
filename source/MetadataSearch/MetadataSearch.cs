using MetadataSearch.SearchItems;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MetadataSearch;

public class MetadataSearch : GenericPlugin
{
    public MetadataSearch(IPlayniteAPI playniteAPI) : base(playniteAPI)
    {
        Searches = [
            new("fp", "Filter presets", new DatabaseObjectFilterSearchContext<FilterPreset, FilterPresetSearchItem>(playniteAPI, pn=>pn.Database.FilterPresets, x=>new(playniteAPI.MainView, x)){Label = "Filter presets"}),
            new("library", "Filter by library", new DatabaseObjectFilterSearchContext<DatabaseObject, LibraryFilterSearchItem>(playniteAPI, pn=>pn.Addons.Plugins.OfType<LibraryPlugin>().Select(p=>new DatabaseObject{Id=p.Id,Name=p.Name }), x=>new(playniteAPI.MainView, x)){Label = "Libraries"}),
            new("agerating", "Filter by age rating", new DatabaseObjectFilterSearchContext<AgeRating, AgeRatingFilterSearchItem>(playniteAPI, pn=>pn.Database.AgeRatings, x=>new(playniteAPI.MainView, x)){Label = "Age ratings"}),
            new("category", "Filter by category", new DatabaseObjectFilterSearchContext<Category, CategoryFilterSearchItem>(playniteAPI, pn=>pn.Database.Categories, x=>new(playniteAPI.MainView, x)){Label = "Categories"}),
            new("completion", "Filter by completion status", new DatabaseObjectFilterSearchContext<CompletionStatus, CompletionStatusFilterSearchItem>(playniteAPI, pn=>pn.Database.CompletionStatuses, x=>new(playniteAPI.MainView, x)){Label = "Completion statuses"}),
            new("developer", "Filter by developer", new DatabaseObjectFilterSearchContext<Company, DeveloperFilterSearchItem>(playniteAPI, pn=>pn.Database.Companies, x=>new(playniteAPI.MainView, x)){Label = "Developers"}),
            new("publisher", "Filter by publisher", new DatabaseObjectFilterSearchContext<Company, PublisherFilterSearchItem>(playniteAPI, pn=>pn.Database.Companies, x=>new(playniteAPI.MainView, x)){Label = "Publishers"}),
            new("feature", "Filter by feature", new DatabaseObjectFilterSearchContext<GameFeature, FeatureFilterSearchItem>(playniteAPI, pn=>pn.Database.Features, x=>new(playniteAPI.MainView, x)){Label = "Features"}),
            new("genre", "Filter by genre", new DatabaseObjectFilterSearchContext<Genre, GenreFilterSearchItem>(playniteAPI, pn=>pn.Database.Genres, x=>new(playniteAPI.MainView, x)){Label = "Genres"}),
            new("platform", "Filter by platform", new DatabaseObjectFilterSearchContext<Platform, PlatformFilterSearchItem>(playniteAPI, pn=>pn.Database.Platforms, x=>new(playniteAPI.MainView, x)){Label = "Platforms"}),
            new("region", "Filter by region", new DatabaseObjectFilterSearchContext<Region, RegionFilterSearchItem>(playniteAPI, pn=>pn.Database.Regions, x=>new(playniteAPI.MainView, x)){Label = "Regions"}),
            new("series", "Filter by series", new DatabaseObjectFilterSearchContext<Series, SeriesFilterSearchItem>(playniteAPI, pn=>pn.Database.Series, x=>new(playniteAPI.MainView, x)){Label = "Series"}),
            new("source", "Filter by source", new DatabaseObjectFilterSearchContext<GameSource, SourceFilterSearchItem>(playniteAPI, pn=>pn.Database.Sources, x=>new(playniteAPI.MainView, x)){Label = "Sources"}),
            new("tag", "Filter by tag", new DatabaseObjectFilterSearchContext<Tag,TagFilterSearchItem>(playniteAPI, pn=>pn.Database.Tags, x=>new(playniteAPI.MainView, x)){Label = "Tags"})
        ];
    }

    public override Guid Id { get; } = new("bd6bdf7f-86d8-4fa9-b056-29c8753d475f");

    public override IEnumerable<SearchItem> GetSearchGlobalCommands()
    {
        foreach (var filterPreset in PlayniteApi.Database.FilterPresets)
            yield return new FilterPresetSearchItem(PlayniteApi.MainView, filterPreset);
    }
}
