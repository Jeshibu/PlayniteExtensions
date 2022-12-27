using GiantBombMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GiantBombMetadata
{
    class GiantBombExtraMetadataProvider
    {
        public GiantBombExtraMetadataProvider(IPlayniteAPI playniteAPI, GiantBombMetadataSettings settings)
        {
            PlayniteApi = playniteAPI;
            Settings = settings;
        }

        private ILogger logger = LogManager.GetLogger();
        public IPlayniteAPI PlayniteApi { get; }
        public GiantBombMetadataSettings Settings { get; }

        public void ImportGameProperty()
        {
            var apiClient = new GiantBombApiClient { ApiKey = Settings.ApiKey };

            var selectedItem = SelectGiantBombGameProperty(apiClient);

            if (selectedItem == null)
                return;

            var itemDetails = apiClient.GetGameProperty($"{selectedItem.ResourceType}/{selectedItem.Guid}");

            if (itemDetails == null)
                return;

            var viewModel = PromptGamePropertyImportUserApproval(selectedItem, itemDetails);

            if (viewModel == null)
                return;

            UpdateGames(viewModel);
        }

        private GiantBombSearchResultItem SelectGiantBombGameProperty(GiantBombApiClient apiClient)
        {
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

            return selectedItem?.SearchResultItem;
        }

        private GamePropertyImportViewModel PromptGamePropertyImportUserApproval(GiantBombSearchResultItem selectedItem, GiantBombGamePropertyDetails itemDetails)
        {
            var matchingGames = new List<GameCheckboxViewModel>();
            var snc = new SortableNameConverter(new string[0], batchOperation: itemDetails.Games.Length + PlayniteApi.Database.Games.Count > 100, numberLength: 1, removeEditions: true);
            var deflatedNames = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
            {
                a.ProgressMaxValue = itemDetails.Games.Length;
                for (int i = 0; i < itemDetails.Games.Length; i++)
                {
                    if (a.CancelToken.IsCancellationRequested)
                        return;

                    //if (i % 4 == 0)
                    a.CurrentProgressValue = i + 1;

                    var gbGame = itemDetails.Games[i];

                    if (!deflatedNames.TryGetValue(gbGame.Name, out string nameToMatch))
                    {
                        nameToMatch = snc.Convert(gbGame.Name).Deflate();
                        deflatedNames.Add(gbGame.Name, nameToMatch);
                    }

                    var gbGuid = GiantBombHelper.GetGiantBomgGuidFromUrl(gbGame.SiteDetailUrl);
                    foreach (var g in PlayniteApi.Database.Games)
                    {
                        var guid = GiantBombHelper.GetGiantBombGuidFromGameLinks(g);
                        if (guid != null)
                        {
                            if (guid == gbGuid)
                                matchingGames.Add(new GameCheckboxViewModel(g, gbGame));

                            continue;
                        }

                        if (!deflatedNames.TryGetValue(g.Name, out string gName))
                        {
                            gName = snc.Convert(g.Name).Deflate();
                            deflatedNames.Add(g.Name, gName);
                        }

                        if (string.Equals(nameToMatch, gName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            matchingGames.Add(new GameCheckboxViewModel(g, gbGame));
                        }
                    }
                }
                matchingGames = matchingGames.OrderBy(g => g.Game.Name).ThenBy(g => g.Game.ReleaseDate).ToList();
            }, new GlobalProgressOptions("Matching game titles...", true) { IsIndeterminate = false });

            if (matchingGames.Count == 0)
            {
                PlayniteApi.Dialogs.ShowMessage("No matching games found in your library.", "Giant Bomb", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return null;
            }

            GiantBombPropertyImportSetting importSetting;
            switch (selectedItem.ResourceType)
            {
                case "character":
                    importSetting = Settings.Characters;
                    break;
                case "concept":
                    importSetting = Settings.Concepts;
                    break;
                case "object":
                    importSetting = Settings.Objects;
                    break;
                case "location":
                    importSetting = Settings.Locations;
                    break;
                case "person":
                    importSetting = Settings.People;
                    break;
                default:
                    logger.Error($"Unknown resource type: {selectedItem.ResourceType}");
                    return null;
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
            if (dialogResult == true)
                return viewModel;
            else
                return null;
        }

        private void UpdateGames(GamePropertyImportViewModel viewModel)
        {
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
                        {
                            g.Game.Links.Add(new Link("Giant Bomb", g.GiantBombData.SiteDetailUrl));
                            update = true;
                        }
                    }

                    if (update)
                    {
                        g.Game.Modified = DateTime.Now;
                        PlayniteApi.Database.Games.Update(g.Game);
                    }
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
                if (!string.IsNullOrEmpty(item.Deck))
                    Description = $"{item.ResourceType.ToUpper()}\n{item.Deck}";
            }

            public GiantBombSearchResultItem SearchResultItem { get; }
        }
    }
}
