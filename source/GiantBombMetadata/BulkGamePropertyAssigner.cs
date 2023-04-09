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
using System.Windows;

namespace GiantBombMetadata
{

    public class GiantBombGamePropertyAssigner : BulkGamePropertyAssigner<GiantBombSearchResultItem>
    {
        public GiantBombMetadataSettings Settings { get; }

        public override string MetadataProviderName => "Giant Bomb";

        public GiantBombGamePropertyAssigner(IPlayniteAPI playniteAPI, GiantBombMetadataSettings settings, GiantBombGamePropertySearchProvider dataSource) : base(playniteAPI, dataSource)
        {
            Settings = settings;
        }

        protected override PropertyImportSetting GetPropertyImportSetting(GiantBombSearchResultItem selectedItem)
        {
            switch (selectedItem.ResourceType)
            {
                case "character":
                    return Settings.Characters;
                case "concept":
                    return Settings.Concepts;
                case "object":
                    return Settings.Objects;
                case "location":
                    return Settings.Locations;
                case "person":
                    return Settings.People;
                default:
                    logger.Error($"Unknown resource type: {selectedItem.ResourceType}");
                    return null;
            }
        }

        protected override string GetGameIdFromUrl(string url)
        {
            return GiantBombHelper.GetGiantBombGuidFromUrl(url);
        }
    }

    public class PropertyImportSetting
    {
        public string Prefix { get; set; }
        public PropertyImportTarget ImportTarget { get; set; }
    }

    public enum PropertyImportTarget
    {
        Ignore,
        Genres,
        Tags,
    }

    public interface IHasName
    {
        string Name { get; }
    }

    public abstract class BulkGamePropertyAssigner<TSearchItem> where TSearchItem : IHasName
    {
        public BulkGamePropertyAssigner(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<TSearchItem, IEnumerable<GameDetails>> dataSource)
        {
            PlayniteApi = playniteAPI;
            this.dataSource = dataSource;
        }

        protected readonly ILogger logger = LogManager.GetLogger();
        protected readonly ISearchableDataSourceWithDetails<TSearchItem, IEnumerable<GameDetails>> dataSource;
        public abstract string MetadataProviderName { get; }

        public IPlayniteAPI PlayniteApi { get; }

        public void ImportGameProperty()
        {
            var selectedItem = SelectGiantBombGameProperty();

            if (selectedItem == null)
                return;

            var associatedGames = dataSource.GetDetails(selectedItem)?.ToList();

            if (associatedGames == null)
                return;

            var viewModel = PromptGamePropertyImportUserApproval(selectedItem, associatedGames);

            if (viewModel == null)
                return;

            UpdateGames(viewModel);
        }

        private TSearchItem SelectGiantBombGameProperty()
        {
            var selectedItem = PlayniteApi.Dialogs.ChooseItemWithSearch(null, (a) =>
            {
                var output = new List<GenericItemOption>();

                if (string.IsNullOrWhiteSpace(a))
                    return output;

                try
                {
                    var searchResult = dataSource.Search(a);
                    output.AddRange(searchResult.Select(dataSource.ToGenericItemOption));
                }
                catch (Exception e)
                {
                    logger.Error(e, $"Failed to get search data for <{a}>");
                    return new List<GenericItemOption>();
                }

                return output;
            }, string.Empty, "Search for a property to assign to all your matching games") as GenericItemOption<TSearchItem>;

            return selectedItem == null ? default : selectedItem.Item;
        }

        protected abstract PropertyImportSetting GetPropertyImportSetting(TSearchItem searchItem);

        protected abstract string GetGameIdFromUrl(string url);

        private GamePropertyImportViewModel PromptGamePropertyImportUserApproval(TSearchItem selectedItem, List<GameDetails> gamesToMatch)
        {
            var importSetting = GetPropertyImportSetting(selectedItem);
            if (importSetting == null)
            {
                logger.Error($"Could not find import settings for game property <{selectedItem.Name}>");
                PlayniteApi.Notifications.Add(this.GetType().Name, "Could not find import settings for property", NotificationType.Error);
                return null;
            }

            var matchingGames = new List<GameCheckboxViewModel>();
            var snc = new SortableNameConverter(new string[0], batchOperation: gamesToMatch.Count + PlayniteApi.Database.Games.Count > 100, numberLength: 1, removeEditions: true);
            var deflatedNames = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
            {
                a.ProgressMaxValue = gamesToMatch.Count;
                for (int i = 0; i < gamesToMatch.Count; i++)
                {
                    if (a.CancelToken.IsCancellationRequested)
                        return;

                    //if (i % 4 == 0)
                    a.CurrentProgressValue = i + 1;

                    var gbGame = gamesToMatch[i];

                    var namesToMatch = new List<string>();
                    foreach (string name in gbGame.Names)
                    {
                        if (!deflatedNames.TryGetValue(name, out string nameToMatch))
                        {
                            nameToMatch = snc.Convert(name).Deflate();
                            deflatedNames.Add(name, nameToMatch);
                        }
                        namesToMatch.Add(nameToMatch);
                    }

                    var gameToMatchId = GetGameIdFromUrl(gbGame.Url);
                    foreach (var g in PlayniteApi.Database.Games)
                    {
                        if (g.Links != null && gameToMatchId != null)
                        {
                            var ids = g.Links.Select(l => GetGameIdFromUrl(l.Url)).Where(x => x != null).ToList();
                            if (ids.Count > 0)
                            {
                                if (ids.Contains(gameToMatchId))
                                    matchingGames.Add(new GameCheckboxViewModel(g, gbGame));

                                continue;
                            }
                        }

                        if (!deflatedNames.TryGetValue(g.Name, out string gName))
                        {
                            gName = snc.Convert(g.Name).Deflate();
                            deflatedNames.Add(g.Name, gName);
                        }

                        if (namesToMatch.Any(n => string.Equals(n, gName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            matchingGames.Add(new GameCheckboxViewModel(g, gbGame));
                        }
                    }
                }
                matchingGames = matchingGames.OrderBy(g => string.IsNullOrWhiteSpace(g.Game.SortingName) ? g.Game.Name : g.Game.SortingName).ThenBy(g => g.Game.ReleaseDate).ToList();
            }, new GlobalProgressOptions($"Matching {gamesToMatch.Count} games…", true) { IsIndeterminate = false });

            if (matchingGames.Count == 0)
            {
                PlayniteApi.Dialogs.ShowMessage("No matching games found in your library.", $"{MetadataProviderName} game property assigner", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }

            var viewModel = new GamePropertyImportViewModel() { Name = $"{importSetting.Prefix}{selectedItem.Name}", Games = matchingGames };
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
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
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
                    case GamePropertyImportTargetField.Feature:
                        dbItem = PlayniteApi.Database.Features.FirstOrDefault(f => f.Name.Equals(viewModel.Name, StringComparison.InvariantCultureIgnoreCase))
                                 ?? PlayniteApi.Database.Features.Add(viewModel.Name);
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
                        case GamePropertyImportTargetField.Feature:
                            update |= AddItem(g.Game, x => x.FeatureIds, dbItem.Id);
                            break;
                    }

                    if (viewModel.AddLink)
                    {
                        if (g.Game.Links == null)
                            g.Game.Links = new System.Collections.ObjectModel.ObservableCollection<Link>();

                        if (!g.Game.Links.Any(l => l.Url == g.GameDetails.Url))
                        {
                            g.Game.Links.Add(new Link(MetadataProviderName, g.GameDetails.Url));
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
