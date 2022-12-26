using GiantBombMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GiantBombMetadata
{
    public class GiantBombMetadata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public GiantBombMetadataSettingsViewModel Settings { get; set; }

        public IPlatformUtility PlatformUtility { get; set; }

        public override Guid Id { get; } = Guid.Parse("975c7dc6-efd5-41d4-b9c1-9394b3bfe9c6");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Description,
            MetadataField.Tags,
            MetadataField.Platform,
            MetadataField.ReleaseDate,
            MetadataField.Name,
            MetadataField.Genres,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Series,
            MetadataField.AgeRating,
            MetadataField.Links,
            MetadataField.CoverImage,
        };

        public override string Name { get; } = "Giant Bomb";

        public GiantBombMetadata(IPlayniteAPI api) : base(api)
        {
            Settings = new GiantBombMetadataSettingsViewModel(this);
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
            PlatformUtility = new PlatformUtility(api);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new GiantBombMetadataProvider(options, this, new GiantBombApiClient { ApiKey = Settings.Settings.ApiKey }, PlatformUtility);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GiantBombMetadataSettingsView();
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem { Description = "Import Giant Bomb game property", Action = a => ImportGameProperty() };
        }

        public void ImportGameProperty()
        {
            var apiClient = new GiantBombApiClient { ApiKey = Settings.Settings.ApiKey };
            var selectedItem = PlayniteApi.Dialogs.ChooseItemWithSearch(null, (a) =>
            {
                try
                {
                    var searchResult = apiClient.SearchGameProperties(a);
                    return searchResult.Select(x => new GiantBombSearchResultItemOption(x)).ToList<GenericItemOption>();
                }
                catch (Exception e)
                {
                    logger.Error(e, $"Failed to get Giant Bomb search data for <{a}>");
                    return new List<GenericItemOption>();
                }
            }, string.Empty, "Search for a piece of Giant Bomb metadata to assign to all your matching games") as GiantBombSearchResultItemOption;

            if (selectedItem == null)
                return;

            var itemDetails = apiClient.GetGameProperty($"{selectedItem.SearchResultItem.ResourceType}/{selectedItem.SearchResultItem.Guid}");

            if (itemDetails == null)
                return;

            var matchingGames = new List<GameCheckboxViewModel>();
            PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
            {
                a.ProgressMaxValue = itemDetails.Games.Length;
                foreach (var gbGame in itemDetails.Games)
                {
                    a.CurrentProgressValue++;
                    var gbName = gbGame.Name.Deflate();
                    foreach (var g in PlayniteApi.Database.Games)
                    {
                        if (string.Equals(gbName, g.Name.Deflate(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            matchingGames.Add(new GameCheckboxViewModel(g, gbGame));
                        }
                    }
                }
                matchingGames = matchingGames.OrderBy(g => g.Game.Name).ThenBy(g => g.Game.ReleaseDate).ToList();
            }, new GlobalProgressOptions("Matching game titles...", true) { IsIndeterminate = false });

            GiantBombPropertyImportSetting importSetting;
            switch (selectedItem.SearchResultItem.ResourceType)
            {
                case "character":
                    importSetting = Settings.Settings.Characters;
                    break;
                case "concept":
                    importSetting = Settings.Settings.Concepts;
                    break;
                case "object":
                    importSetting = Settings.Settings.Objects;
                    break;
                case "location":
                    importSetting = Settings.Settings.Locations;
                    break;
                case "person":
                    importSetting = Settings.Settings.People;
                    break;
                default:
                    logger.Error($"Unknown resource type: {selectedItem.SearchResultItem.ResourceType}");
                    return;
            }

            var viewModel = new GamePropertyImportViewModel() { Name = $"{importSetting.Prefix}{itemDetails.Name}", Games = matchingGames };
            switch (importSetting.ImportTarget)
            {
                case PropertyImportTarget.Genres:
                    viewModel.TargetField = GamePropertyImportTargetField.Genre;
                    break;
                case PropertyImportTarget.Ignore:
                case PropertyImportTarget.Tags:
                default:
                    viewModel.TargetField = GamePropertyImportTargetField.Tag;
                    break;
            }

            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions { ShowCloseButton = true, ShowMaximizeButton = true, ShowMinimizeButton = false });
            var view = new GamePropertyImportView(window) { DataContext = viewModel };
            window.Content = view;
            window.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
            window.Title = "Select games";
            var dialogResult = window.ShowDialog();
            if (dialogResult != true)
                return;

            using (PlayniteApi.Database.BufferedUpdate())
            {
                DatabaseObject dbItem;
                switch (viewModel.TargetField)
                {
                    case GamePropertyImportTargetField.Category:
                        dbItem = PlayniteApi.Database.Categories.FirstOrDefault(c => c.Name.Equals(viewModel.Name, StringComparison.InvariantCultureIgnoreCase))
                                 ?? PlayniteApi.Database.Categories.Add(viewModel.Name);
                        break;
                    case GamePropertyImportTargetField.Genre:
                        dbItem = PlayniteApi.Database.Genres.FirstOrDefault(c => c.Name.Equals(viewModel.Name, StringComparison.InvariantCultureIgnoreCase))
                                 ?? PlayniteApi.Database.Genres.Add(viewModel.Name);
                        break;
                    case GamePropertyImportTargetField.Tag:
                        dbItem = PlayniteApi.Database.Tags.FirstOrDefault(c => c.Name.Equals(viewModel.Name, StringComparison.InvariantCultureIgnoreCase))
                                 ?? PlayniteApi.Database.Tags.Add(viewModel.Name);
                        break;
                    default:
                        throw new ArgumentException();
                }

                foreach (var g in viewModel.Games)
                {
                    if (!g.IsChecked)
                        continue;

                    bool update = false;

                    switch (viewModel.TargetField)
                    {
                        case GamePropertyImportTargetField.Category:
                            update |= AddItem(g.Game, x => x.CategoryIds, dbItem.Id);
                            break;
                        case GamePropertyImportTargetField.Genre:
                            update |= AddItem(g.Game, x => x.GenreIds, dbItem.Id);
                            break;
                        case GamePropertyImportTargetField.Tag:
                            update |= AddItem(g.Game, x => x.TagIds, dbItem.Id);
                            break;
                    }

                    if (viewModel.AddLink)
                    {
                        if (g.Game.Links == null)
                            g.Game.Links = new System.Collections.ObjectModel.ObservableCollection<Link>();

                        if (!g.Game.Links.Any(l => l.Url == g.GiantBombData.SiteDetailUrl))
                            g.Game.Links.Add(new Link("Giant Bomb", g.GiantBombData.SiteDetailUrl));

                        update = true;
                    }

                    if (update)
                        PlayniteApi.Database.Games.Update(g.Game);
                }
            }
        }

        private bool AddItem(Game g, Expression<Func<Game, List<Guid>>> collectionSelector, Guid idToAdd)
        {
            var collection = collectionSelector.Compile()(g);
            if (collection == null)
            {
                collection = new List<Guid>();
                var prop = (PropertyInfo)((MemberExpression)collectionSelector.Body).Member;
                prop.SetValue(g, collection, null);
            }
            else if (collection.Contains(idToAdd))
            {
                return false;
            }
            collection.Add(idToAdd);
            return true;
        }

        private class GiantBombSearchResultItemOption : GenericItemOption
        {
            public GiantBombSearchResultItemOption(GiantBombSearchResultItem item) : base(item.Name, item.ResourceType)
            {
                SearchResultItem = item;
            }

            public GiantBombSearchResultItem SearchResultItem { get; }
        }
    }
}