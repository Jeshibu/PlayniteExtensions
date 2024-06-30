using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Windows;
using PlayniteExtensions.Common;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.ObjectModel;

namespace PlayniteExtensions.Metadata.Common
{
    public interface IHasName
    {
        string Name { get; }
    }

    public abstract class BulkGamePropertyAssigner<TSearchItem, TApprovalPromptViewModel>
        where TSearchItem : IHasName
        where TApprovalPromptViewModel : GamePropertyImportViewModel, new()
    {
        public BulkGamePropertyAssigner(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<TSearchItem, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism = 8)
        {
            playniteApi = playniteAPI;
            this.dataSource = dataSource;
            this.platformUtility = platformUtility;
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        protected readonly ILogger logger = LogManager.GetLogger();
        protected readonly ISearchableDataSourceWithDetails<TSearchItem, IEnumerable<GameDetails>> dataSource;
        private readonly IPlatformUtility platformUtility;
        protected readonly IPlayniteAPI playniteApi;
        public abstract string MetadataProviderName { get; }
        protected bool AllowEmptySearchQuery { get; set; } = false;
        public int MaxDegreeOfParallelism { get; }

        protected virtual GlobalProgressOptions GetGameDownloadProgressOptions(TSearchItem selectedItem)
        {
            return new GlobalProgressOptions("Downloading list of associated games", cancelable: true) { IsIndeterminate = true };
        }

        public void ImportGameProperty()
        {
            var selectedItem = SelectGameProperty();

            if (selectedItem == null)
                return;

            List<GameDetails> associatedGames = null;
            playniteApi.Dialogs.ActivateGlobalProgress(a =>
            {
                associatedGames = dataSource.GetDetails(selectedItem, a)?.ToList();
            }, GetGameDownloadProgressOptions(selectedItem));

            if (associatedGames == null)
                return;

            var viewModel = PromptGamePropertyImportUserApproval(selectedItem, associatedGames);

            if (viewModel == null)
                return;

            UpdateGames(viewModel);
        }

        private TSearchItem SelectGameProperty()
        {
            var selectedItem = playniteApi.Dialogs.ChooseItemWithSearch(null, (a) =>
            {
                var output = new List<GenericItemOption>();

                if (!AllowEmptySearchQuery && string.IsNullOrWhiteSpace(a))
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

        protected abstract PropertyImportSetting GetPropertyImportSetting(TSearchItem searchItem, out string name);

        protected abstract string GetGameIdFromUrl(string url);

        protected virtual string GetIdFromGameLibrary(Guid libraryPluginId, string gameId) => null;

        private IDictionary<string, IList<Game>> GetLibraryGamesById(CancellationToken cancellationToken, out IReadOnlyCollection<Game> unmatchedGames)
        {
            var gamesById = new ConcurrentDictionary<string, IList<Game>>(StringComparer.InvariantCultureIgnoreCase);
            var umg = new ConcurrentBag<Game>();

            var options = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = MaxDegreeOfParallelism };

            Parallel.ForEach(playniteApi.Database.Games, options, game =>
            {
                var idList = game.Links?.Select(l => GetGameIdFromUrl(l.Url)).Where(x => x != null).ToList() ?? new List<string>();
                var libraryGameId = GetIdFromGameLibrary(game.PluginId, game.GameId);
                if (libraryGameId != null)
                    idList.Add(libraryGameId);

                var ids = new HashSet<string>(idList, StringComparer.InvariantCultureIgnoreCase);

                if (ids.Any())
                {
                    foreach (var id in ids)
                    {
                        gamesById.AddOrUpdate(id, new List<Game> { game },
                            (string i, IList<Game> existing) => { existing.Add(game); return existing; });
                    }
                }
                else
                {
                    umg.Add(game);
                }
            });

            unmatchedGames = umg.ToList();

            return gamesById;
        }

        private GamePropertyImportViewModel PromptGamePropertyImportUserApproval(TSearchItem selectedItem, List<GameDetails> gamesToMatch)
        {
            var importSetting = GetPropertyImportSetting(selectedItem, out string propName);
            if (importSetting == null)
            {
                logger.Error($"Could not find import settings for game property <{selectedItem.Name}>");
                playniteApi.Notifications.Add(this.GetType().Name, "Could not find import settings for property", NotificationType.Error);
                return null;
            }

            var proposedMatches = new ConcurrentDictionary<Guid, GameCheckboxViewModel>();
            var snc = new SortableNameConverter(new string[0], numberLength: 1, removeEditions: true);
            var deflatedNames = new ConcurrentDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            bool loopCompleted = false;
            playniteApi.Dialogs.ActivateGlobalProgress(a =>
            {
                a.ProgressMaxValue = gamesToMatch.Count + 2;

                var gamesById = GetLibraryGamesById(a.CancelToken, out var gamesWithNoKnownId);
                a.CurrentProgressValue++;

                GetDeflatedNames(gamesWithNoKnownId.Select(g => g.Name), deflatedNames, snc);
                a.CurrentProgressValue++;

                ParallelOptions parallelOptions = new ParallelOptions() { CancellationToken = a.CancelToken, MaxDegreeOfParallelism = MaxDegreeOfParallelism };
                var loopResult = Parallel.For(0, gamesToMatch.Count, parallelOptions, i =>
                {
                    var externalGameInfo = gamesToMatch[i];
                    externalGameInfo.Id = GetGameIdFromUrl(externalGameInfo.Url);

                    void AddMatchedGame(Game game)
                    {
                        var added = proposedMatches.TryAdd(game.Id, new GameCheckboxViewModel(game, externalGameInfo));
                        if (!added)
                        {
                            var firstMatch = proposedMatches[game.Id];
                            logger.Info($"Skipped adding ${game.Name} again with {externalGameInfo}, already matched with {firstMatch.GameDetails}");
                        }
                    }

                    if (externalGameInfo.Id != null && gamesById.TryGetValue(externalGameInfo.Id, out var gamesWithThisId))
                    {
                        foreach (var g in gamesWithThisId)
                            AddMatchedGame(g);
                    }

                    var namesToMatch = GetDeflatedNames(externalGameInfo.Names, deflatedNames, snc).ToList();

                    foreach (var g in gamesWithNoKnownId)
                    {
                        var libraryGameNameDeflated = deflatedNames[g.Name];

                        if (namesToMatch.Contains(libraryGameNameDeflated, StringComparer.InvariantCultureIgnoreCase)
                            && platformUtility.PlatformsOverlap(g.Platforms, externalGameInfo.Platforms))
                        {
                            AddMatchedGame(g);
                        }
                    }

                    a.CurrentProgressValue++;
                });
                loopCompleted = loopResult.IsCompleted;
            }, new GlobalProgressOptions($"Matching {gamesToMatch.Count} games…", true) { IsIndeterminate = false });

            if (!loopCompleted)
                return null;

            if (proposedMatches.Count == 0)
            {
                playniteApi.Dialogs.ShowMessage("No matching games found in your library.", $"{MetadataProviderName} game property assigner", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }

            var matchingGames = proposedMatches.Values.OrderBy(g => string.IsNullOrWhiteSpace(g.Game.SortingName) ? g.Game.Name : g.Game.SortingName).ThenBy(g => g.Game.ReleaseDate).ToList();

            var viewModel = new TApprovalPromptViewModel() { Name = $"{importSetting.Prefix}{propName}", Games = matchingGames, PlayniteAPI = playniteApi };
            viewModel.Links.AddRange(GetPotentialLinks(selectedItem));
            viewModel.Filters.AddRange(GetCheckboxFilters(viewModel));
            switch (importSetting.ImportTarget)
            {
                case PropertyImportTarget.Genres:
                    viewModel.TargetField = GamePropertyImportTargetField.Genre;
                    break;
                case PropertyImportTarget.Series:
                    viewModel.TargetField = GamePropertyImportTargetField.Series;
                    break;
                case PropertyImportTarget.Features:
                    viewModel.TargetField = GamePropertyImportTargetField.Feature;
                    break;
                case PropertyImportTarget.Ignore:
                case PropertyImportTarget.Tags:
                default:
                    viewModel.TargetField = GamePropertyImportTargetField.Tag;
                    break;
            }

            var window = playniteApi.Dialogs.CreateWindow(new WindowCreationOptions { ShowCloseButton = true, ShowMaximizeButton = true, ShowMinimizeButton = false });
            var view = GetBulkPropertyImportView(window, viewModel);
            window.Content = view;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Title = "Select games";
            window.SizeChanged += Window_SizeChanged;
            var dialogResult = window.ShowDialog();
            if (dialogResult == true)
                return viewModel;
            else
                return null;
        }

        private static IEnumerable<string> GetDeflatedNames(IEnumerable<string> names, ConcurrentDictionary<string, string> deflatedNames, SortableNameConverter snc)
        {
            var output = new HashSet<string>();
            foreach (string name in names)
            {
                if (!deflatedNames.TryGetValue(name, out string nameToMatch))
                {
                    nameToMatch = snc.Convert(name).Deflate();
                    deflatedNames.TryAdd(name, nameToMatch);
                }
                output.Add(nameToMatch);
            }
            return output;
        }

        private bool windowSizedDown = false;

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (windowSizedDown) return;

            var window = sender as Window;

            if (window == null) return;

            var screen = System.Windows.Forms.Screen.AllScreens.OrderBy(s => s.WorkingArea.Height).First();
            var dpi = VisualTreeHelper.GetDpi(window);

            if (window.ActualHeight * dpi.DpiScaleY > screen.WorkingArea.Height)
            {
                windowSizedDown = true;
                window.SizeToContent = SizeToContent.Width;
                window.Height = 0.96D * screen.WorkingArea.Height / dpi.DpiScaleY;
            }
        }

        protected virtual UserControl GetBulkPropertyImportView(Window window, TApprovalPromptViewModel viewModel)
        {
            return new GamePropertyImportView(window) { DataContext = viewModel };
        }

        private void UpdateGames(GamePropertyImportViewModel viewModel)
        {
            using (playniteApi.Database.BufferedUpdate())
            {
                var dbItem = GetDatabaseObject(viewModel);

                foreach (var g in viewModel.Games)
                {
                    if (!g.IsChecked)
                        continue;

                    bool update = AddItem(g.Game, viewModel.TargetField, dbItem.Id);

                    foreach (var link in viewModel.Links)
                    {
                        if (!link.Checked)
                            continue;

                        if (g.Game.Links == null)
                            g.Game.Links = new ObservableCollection<Link>();

                        var url = link.GetUrl(g.GameDetails);

                        if (link.IsAlreadyLinked(g.Game.Links, url))
                            continue;

                        g.Game.Links.Add(new Link(link.Name, url));
                        update = true;
                    }

                    if (update)
                    {
                        g.Game.Modified = DateTime.Now;
                        playniteApi.Database.Games.Update(g.Game);
                    }
                }
            }
        }

        private static DatabaseObject GetDatabaseObject(GamePropertyImportViewModel viewModel)
        {
            var db = viewModel.PlayniteAPI.Database;
            switch (viewModel.TargetField)
            {
                case GamePropertyImportTargetField.Category:
                    return GetDatabaseObjectByName(db.Categories, viewModel.Name);
                case GamePropertyImportTargetField.Genre:
                    return GetDatabaseObjectByName(db.Genres, viewModel.Name);
                case GamePropertyImportTargetField.Tag:
                    return GetDatabaseObjectByName(db.Tags, viewModel.Name);
                case GamePropertyImportTargetField.Feature:
                    return GetDatabaseObjectByName(db.Features, viewModel.Name);
                case GamePropertyImportTargetField.Series:
                    return GetDatabaseObjectByName(db.Series, viewModel.Name);
                default:
                    throw new ArgumentException();
            }
        }

        private static DatabaseObject GetDatabaseObjectByName<T>(IItemCollection<T> collection, string name) where T : DatabaseObject
        {
            return collection.FirstOrDefault(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                   ?? collection.Add(name);
        }


        protected virtual IEnumerable<PotentialLink> GetPotentialLinks(TSearchItem searchItem)
        {
            yield return new PotentialLink(MetadataProviderName, game => game.Url);
        }

        protected virtual IEnumerable<CheckboxFilter> GetCheckboxFilters(GamePropertyImportViewModel viewModel)
        {
            yield return new CheckboxFilter("Check all", viewModel, x => true);
            yield return new CheckboxFilter("Uncheck all", viewModel, x => false);
            yield return new CheckboxFilter("Only filtered games", viewModel, x => playniteApi.MainView.FilteredGames.Contains(x.Game));
        }

        private static bool AddItem(Game g, Expression<Func<Game, List<Guid>>> collectionSelector, Guid idToAdd)
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

        private static Expression<Func<Game, List<Guid>>> GetCollectionSelector(GamePropertyImportTargetField targetField)
        {
            switch (targetField)
            {
                case GamePropertyImportTargetField.Category:
                    return x => x.CategoryIds;
                case GamePropertyImportTargetField.Genre:
                    return x => x.GenreIds;
                case GamePropertyImportTargetField.Tag:
                    return x => x.TagIds;
                case GamePropertyImportTargetField.Feature:
                    return x => x.FeatureIds;
                case GamePropertyImportTargetField.Series:
                    return x => x.SeriesIds;
                default:
                    throw new ArgumentException($"Unknown target field: {targetField}");
            }
        }

        private static bool AddItem(Game g, GamePropertyImportTargetField targetField, Guid idToAdd) => AddItem(g, GetCollectionSelector(targetField), idToAdd);
    }
}
