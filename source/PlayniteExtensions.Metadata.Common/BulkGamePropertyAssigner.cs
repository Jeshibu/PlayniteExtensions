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

namespace PlayniteExtensions.Metadata.Common;

public interface IHasName
{
    string Name { get; }
}

public abstract class BulkGamePropertyAssigner<TSearchItem, TApprovalPromptViewModel>(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<TSearchItem, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, IExternalDatabaseIdUtility databaseIdUtility, ExternalDatabase databaseType, int maxDegreeOfParallelism = 8)
    where TSearchItem : IHasName
    where TApprovalPromptViewModel : GamePropertyImportViewModel, new()
{
    protected readonly ILogger logger = LogManager.GetLogger();
    protected readonly ISearchableDataSourceWithDetails<TSearchItem, IEnumerable<GameDetails>> dataSource = dataSource;
    protected readonly IPlayniteAPI playniteApi = playniteAPI;
    public abstract string MetadataProviderName { get; }
    public bool AllowEmptySearchQuery { get; set; } = false;
    public IExternalDatabaseIdUtility DatabaseIdUtility { get; } = databaseIdUtility;
    public ExternalDatabase DatabaseType { get; } = databaseType;
    public int MaxDegreeOfParallelism { get; } = maxDegreeOfParallelism;

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

    protected virtual TSearchItem SelectGameProperty()
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
                return [];
            }

            return output;
        }, string.Empty, "Search for a property to assign to all your matching games") as GenericItemOption<TSearchItem>;

        return selectedItem == null ? default : selectedItem.Item;
    }

    protected abstract PropertyImportSetting GetPropertyImportSetting(TSearchItem searchItem, out string name);

    protected abstract string GetGameIdFromUrl(string url);

    private GamePropertyImportViewModel PromptGamePropertyImportUserApproval(TSearchItem selectedItem, List<GameDetails> gamesToMatch)
    {
        var importSetting = GetPropertyImportSetting(selectedItem, out string propName);
        if (importSetting == null)
        {
            logger.Error($"Could not find import settings for game property <{selectedItem.Name}>");
            playniteApi.Notifications.Add(this.GetType().Name, "Could not find import settings for property", NotificationType.Error);
            return null;
        }

        var proposedMatches = GetProposedMatches(gamesToMatch).ToList();

        if (proposedMatches.Count == 0)
        {
            playniteApi.Dialogs.ShowMessage("No matching games found in your library.", $"{MetadataProviderName} game property assigner", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        var viewModel = new TApprovalPromptViewModel() { Name = $"{importSetting.Prefix}{propName}", Games = proposedMatches, PlayniteAPI = playniteApi };
        viewModel.Links.AddRange(GetPotentialLinks(selectedItem));
        viewModel.Filters.AddRange(GetCheckboxFilters(viewModel));
        viewModel.TargetField = importSetting.ImportTarget switch
        {
            PropertyImportTarget.Genres => GamePropertyImportTargetField.Genre,
            PropertyImportTarget.Series => GamePropertyImportTargetField.Series,
            PropertyImportTarget.Features => GamePropertyImportTargetField.Feature,
            PropertyImportTarget.Developers => GamePropertyImportTargetField.Developers,
            PropertyImportTarget.Publishers => GamePropertyImportTargetField.Publishers,
            _ => GamePropertyImportTargetField.Tag,
        };
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

    protected virtual IList<DbId> GetIds(GameDetails gameDetails)
    {
        var output = new List<DbId>(gameDetails.ExternalIds);
        var id = gameDetails.Id ?? GetGameIdFromUrl(gameDetails.Url);
        if (id != null)
            output.Add(new DbId(DatabaseType, id));

        return output;
    }

    private IEnumerable<GameCheckboxViewModel> GetProposedMatches(List<GameDetails> gamesToMatch)
    {
        var proposedMatches = new ConcurrentDictionary<Guid, GameCheckboxViewModel>();
        bool loopCompleted = false;
        var progressResult = playniteApi.Dialogs.ActivateGlobalProgress(a =>
        {
            a.ProgressMaxValue = gamesToMatch.Count + 10;

            var matchHelper = new GameMatchingHelper(DatabaseIdUtility, MaxDegreeOfParallelism);
            matchHelper.Prepare(playniteApi.Database.Games, a.CancelToken);
            a.CurrentProgressValue += 10;

            ParallelOptions parallelOptions = new() { CancellationToken = a.CancelToken, MaxDegreeOfParallelism = MaxDegreeOfParallelism };
            var loopResult = Parallel.ForEach(gamesToMatch, parallelOptions, externalGameInfo =>
            {
                try
                {
                    void AddMatchedGame(Game game)
                    {
                        proposedMatches.AddOrUpdate(game.Id, new GameCheckboxViewModel(game, externalGameInfo), (id, existingCheckboxVM) =>
                        {
                            existingCheckboxVM.GameDetails.Add(externalGameInfo);

                            return existingCheckboxVM;
                        });
                    }

                    foreach (var dbId in GetIds(externalGameInfo))
                    {
                        if (matchHelper.TryGetGamesById(dbId, out var gamesWithThisId))
                            foreach (var g in gamesWithThisId)
                                AddMatchedGame(g);
                    }

                    foreach (var name in externalGameInfo.Names)
                    {
                        if (matchHelper.TryGetGamesByName(name, out var gamesWithThisName))
                            foreach (var g in gamesWithThisName)
                                if (platformUtility.PlatformsOverlap(g.Platforms, externalGameInfo.Platforms))
                                    AddMatchedGame(g);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error matching games");
                }

                a.CurrentProgressValue++;
            });
            loopCompleted = loopResult.IsCompleted;
        }, new GlobalProgressOptions($"Matching {gamesToMatch.Count} games…", true) { IsIndeterminate = false });

        if (!loopCompleted)
            return Enumerable.Empty<GameCheckboxViewModel>();

        var matchingGames = proposedMatches.Values.OrderBy(g => string.IsNullOrWhiteSpace(g.Game.SortingName) ? g.Game.Name : g.Game.SortingName).ThenBy(g => g.Game.ReleaseDate).ToList();
        return matchingGames;
    }

    private bool windowSizedDown = false;

    protected void Window_SizeChanged(object sender, SizeChangedEventArgs e)
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

                foreach (var gd in g.GameDetails)
                    gd.Id ??= GetGameIdFromUrl(gd.Url);

                bool update = AddItem(g.Game, viewModel.TargetField, dbItem.Id);

                foreach (var link in viewModel.Links)
                {
                    if (!link.Checked)
                        continue;

                    if (g.Game.Links == null)
                        g.Game.Links = [];

                    foreach (var gd in g.GameDetails)
                    {
                        var url = link.GetUrl(gd);

                        if (link.IsAlreadyLinked(g.Game.Links, url))
                            continue;

                        g.Game.Links.Add(new Link(link.Name, url));
                        update = true;
                    }
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
        return viewModel.TargetField switch
        {
            GamePropertyImportTargetField.Category => GetDatabaseObjectByName(db.Categories, viewModel.Name),
            GamePropertyImportTargetField.Genre => GetDatabaseObjectByName(db.Genres, viewModel.Name),
            GamePropertyImportTargetField.Tag => GetDatabaseObjectByName(db.Tags, viewModel.Name),
            GamePropertyImportTargetField.Feature => GetDatabaseObjectByName(db.Features, viewModel.Name),
            GamePropertyImportTargetField.Series => GetDatabaseObjectByName(db.Series, viewModel.Name),
            GamePropertyImportTargetField.Developers or GamePropertyImportTargetField.Publishers => GetDatabaseObjectByName(db.Companies, viewModel.Name),
            _ => throw new ArgumentException(),
        };
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
        yield return new CheckboxFilter("Only matching platforms", viewModel, PlatformsOverlap);
    }

    private bool PlatformsOverlap(GameCheckboxViewModel checkbox)
    {
        var gdPlatforms = new List<MetadataProperty>();
        foreach (var gd in checkbox.GameDetails)
            if (gd.Platforms != null)
                gdPlatforms.AddRange(gd.Platforms);

        return platformUtility.PlatformsOverlap(checkbox.Game.Platforms, gdPlatforms);
    }

    private static bool AddItem(Game g, Expression<Func<Game, List<Guid>>> collectionSelector, Guid idToAdd)
    {
        var collection = collectionSelector.Compile()(g);
        if (collection == null)
        {
            collection = [];
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
        return targetField switch
        {
            GamePropertyImportTargetField.Category => x => x.CategoryIds,
            GamePropertyImportTargetField.Genre => x => x.GenreIds,
            GamePropertyImportTargetField.Tag => x => x.TagIds,
            GamePropertyImportTargetField.Feature => x => x.FeatureIds,
            GamePropertyImportTargetField.Series => x => x.SeriesIds,
            GamePropertyImportTargetField.Developers => x => x.DeveloperIds,
            GamePropertyImportTargetField.Publishers => x => x.PublisherIds,
            _ => throw new ArgumentException($"Unknown target field: {targetField}"),
        };
    }

    private static bool AddItem(Game g, GamePropertyImportTargetField targetField, Guid idToAdd) => AddItem(g, GetCollectionSelector(targetField), idToAdd);
}
