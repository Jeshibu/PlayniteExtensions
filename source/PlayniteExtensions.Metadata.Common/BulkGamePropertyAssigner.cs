using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;

namespace PlayniteExtensions.Metadata.Common;

public interface IHasName
{
    string Name { get; }
}

public abstract class BulkGamePropertyAssigner<TSearchItem, TApprovalPromptViewModel>(
    IGameDatabaseAPI playniteDatabase,
    BulkPropertyUserInterface ui,
    IBulkPropertyImportDataSource<TSearchItem> dataSource,
    IPlatformUtility platformUtility,
    IExternalDatabaseIdUtility databaseIdUtility,
    ExternalDatabase databaseType,
    int maxDegreeOfParallelism = 8)
    where TSearchItem : IHasName
    where TApprovalPromptViewModel : GamePropertyImportViewModel, new()
{
    protected readonly ILogger logger = LogManager.GetLogger();
    protected IBulkPropertyImportDataSource<TSearchItem> DataSource { get; } = dataSource;
    protected IGameDatabaseAPI Database { get; } = playniteDatabase;
    protected BulkPropertyUserInterface Ui { get; } = ui;
    public abstract string MetadataProviderName { get; }
    public IExternalDatabaseIdUtility DatabaseIdUtility { get; } = databaseIdUtility;
    public ExternalDatabase DatabaseType { get; } = databaseType;
    public int MaxDegreeOfParallelism { get; } = maxDegreeOfParallelism;

    protected virtual GlobalProgressOptions GetGameDownloadProgressOptions(TSearchItem selectedItem)
    {
        return new("Downloading list of associated games", cancelable: true) { IsIndeterminate = true };
    }

    public void ImportGameProperty()
    {
        var selectedItem = SelectGameProperty();

        if (selectedItem == null)
            return;

        List<GameDetails> associatedGames = null;
        Ui.ShowProgress(a => { associatedGames = DataSource.GetDetails(selectedItem, a)?.ToList(); }, GetGameDownloadProgressOptions(selectedItem));

        if (associatedGames == null)
            return;

        var viewModel = PromptGamePropertyImportUserApproval(selectedItem, associatedGames);

        if (viewModel == null)
            return;

        UpdateGames(viewModel);
    }

    public virtual TSearchItem SelectGameProperty() => Ui.SelectGameProperty(DataSource);

    protected abstract PropertyImportSetting GetPropertyImportSetting(TSearchItem searchItem, out string name);

    protected virtual string GetGameIdFromUrl(string url)
    {
        var dbId = DatabaseIdUtility.GetIdFromUrl(url);

        if (dbId.Database == DatabaseType)
            return dbId.Id;

        return null;
    }

    private GamePropertyImportViewModel PromptGamePropertyImportUserApproval(TSearchItem selectedItem, List<GameDetails> gamesToMatch)
    {
        var importSetting = GetPropertyImportSetting(selectedItem, out string propName);
        if (importSetting == null)
        {
            logger.Error($"Could not find import settings for game property <{selectedItem.Name}>");
            Ui.AddNotification(GetType().Name, "Could not find import settings for property", NotificationType.Error);
            return null;
        }

        var proposedMatches = GetProposedMatches(gamesToMatch).ToList();

        if (proposedMatches.Count == 0)
        {
            Ui.ShowDialog("No matching games found in your library.", $"{MetadataProviderName} game property assigner", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        var viewModel = new TApprovalPromptViewModel { Name = $"{importSetting.Prefix}{propName}", Games = proposedMatches };
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
        return Ui.SelectGames(viewModel);
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
        Ui.ShowProgress(a =>
        {
            a.ProgressMaxValue = gamesToMatch.Count + 10;

            var matchHelper = new GameMatchingHelper(DatabaseIdUtility, MaxDegreeOfParallelism);
            matchHelper.Prepare(Database.Games, a.CancelToken);
            a.CurrentProgressValue += 10;

            ParallelOptions parallelOptions = new() { CancellationToken = a.CancelToken, MaxDegreeOfParallelism = MaxDegreeOfParallelism };
            var loopResult = Parallel.ForEach(gamesToMatch, parallelOptions, externalGameInfo =>
            {
                try
                {
                    void AddMatchedGame(Game game)
                    {
                        proposedMatches.AddOrUpdate(game.Id, new GameCheckboxViewModel(game, externalGameInfo), (_, existingCheckboxVm) =>
                        {
                            existingCheckboxVm.GameDetails.Add(externalGameInfo);

                            return existingCheckboxVm;
                        });
                    }

                    foreach (var dbId in GetIds(externalGameInfo))
                        if (matchHelper.TryGetGamesById(dbId, out var gamesWithThisId))
                            foreach (var g in gamesWithThisId)
                                AddMatchedGame(g);

                    foreach (var name in externalGameInfo.Names)
                        if (matchHelper.TryGetGamesByName(name, out var gamesWithThisName))
                            foreach (var g in gamesWithThisName)
                                if (platformUtility.PlatformsOverlap(g.Platforms, externalGameInfo.Platforms))
                                    AddMatchedGame(g);
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
            return [];

        var matchingGames = proposedMatches.Values.OrderBy(g => string.IsNullOrWhiteSpace(g.Game.SortingName) ? g.Game.Name : g.Game.SortingName).ThenBy(g => g.Game.ReleaseDate).ToList();
        return matchingGames;
    }

    private void UpdateGames(GamePropertyImportViewModel viewModel)
    {
        using var bufferedUpdate = Database.BufferedUpdate();
        var dbItem = GetDatabaseObject(viewModel);

        foreach (var g in viewModel.Games)
        {
            if (!g.IsChecked)
                continue;

            foreach (var gd in g.GameDetails)
                gd.Id ??= GetGameIdFromUrl(gd.Url);

            bool update = AddItem(g.Game, viewModel.TargetField, dbItem.Id);

            foreach (var potentialLink in viewModel.Links)
            {
                if (!potentialLink.Checked)
                    continue;

                g.Game.Links ??= [];

                foreach (var gd in g.GameDetails)
                {
                    var url = potentialLink.GetUrl(gd);

                    if (string.IsNullOrWhiteSpace(url) || potentialLink.IsAlreadyLinked(g.Game.Links, url))
                        continue;

                    g.Game.Links.Add(new(potentialLink.Name, url));
                    update = true;
                }
            }

            if (update)
            {
                g.Game.Modified = DateTime.Now;
                Database.Games.Update(g.Game);
            }
        }
    }

    private DatabaseObject GetDatabaseObject(GamePropertyImportViewModel viewModel) => viewModel.TargetField switch
    {
        GamePropertyImportTargetField.Category => GetDatabaseObjectByName(Database.Categories, viewModel.Name),
        GamePropertyImportTargetField.Genre => GetDatabaseObjectByName(Database.Genres, viewModel.Name),
        GamePropertyImportTargetField.Tag => GetDatabaseObjectByName(Database.Tags, viewModel.Name),
        GamePropertyImportTargetField.Feature => GetDatabaseObjectByName(Database.Features, viewModel.Name),
        GamePropertyImportTargetField.Series => GetDatabaseObjectByName(Database.Series, viewModel.Name),
        GamePropertyImportTargetField.Developers or GamePropertyImportTargetField.Publishers => GetDatabaseObjectByName(Database.Companies, viewModel.Name),
        _ => throw new ArgumentException(),
    };

    private static DatabaseObject GetDatabaseObjectByName<T>(IItemCollection<T> collection, string name) where T : DatabaseObject
    {
        return collection.FirstOrDefault(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
               ?? collection.Add(name);
    }

    protected virtual IEnumerable<PotentialLink> GetPotentialLinks(TSearchItem searchItem)
    {
        yield return new(MetadataProviderName, game => game.Url);
    }

    protected virtual IEnumerable<CheckboxFilter> GetCheckboxFilters(GamePropertyImportViewModel viewModel) =>
    [
        new("Check all", viewModel, _ => true),
        new("Uncheck all", viewModel, _ => false),
        new("Only filtered games", viewModel, x => Ui.GameIsInCurrentFilter(x.Game)),
        new("Only matching platforms", viewModel, PlatformsOverlap)
    ];

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

public class BulkPropertyUserInterface(IPlayniteAPI playniteApi)
{
    protected readonly ILogger logger = LogManager.GetLogger();
    protected bool windowSizedDown;
    public bool AllowEmptySearchQuery { get; set; } = false;
    public string DefaultSearch { get; set; } = null;

    protected void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (windowSizedDown) return;

        if (sender is not Window window) return;

        var screen = Screen.AllScreens.OrderBy(s => s.WorkingArea.Height).First();
        var dpi = VisualTreeHelper.GetDpi(window);

        if (window.ActualHeight * dpi.DpiScaleY > screen.WorkingArea.Height)
        {
            windowSizedDown = true;
            window.SizeToContent = SizeToContent.Width;
            window.Height = 0.96D * screen.WorkingArea.Height / dpi.DpiScaleY;
        }
    }

    public virtual TSearchItem SelectGameProperty<TSearchItem>(ISearchableDataSourceWithDetails<TSearchItem, IEnumerable<GameDetails>> dataSource)
    {
        var selectedItem = playniteApi.Dialogs.ChooseItemWithSearch(null, a =>
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
        }, DefaultSearch, "Search for a property to assign to all your matching games") as GenericItemOption<TSearchItem>;

        return selectedItem == null ? default : selectedItem.Item;
    }

    public virtual GamePropertyImportViewModel SelectGames<TApprovalPromptViewModel>(TApprovalPromptViewModel viewModel) where TApprovalPromptViewModel : GamePropertyImportViewModel, new()
    {
        var window = playniteApi.Dialogs.CreateWindow(new() { ShowCloseButton = true, ShowMaximizeButton = true, ShowMinimizeButton = false });
        var control = GetBulkPropertyImportView(window, viewModel);
        window.Content = control;
        window.SizeToContent = SizeToContent.WidthAndHeight;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.Title = "Select games";
        windowSizedDown = false;
        window.SizeChanged += Window_SizeChanged;
        bool? dialogResult = window.ShowDialog();
        return dialogResult == true ? viewModel : null;
    }

    protected virtual UserControl GetBulkPropertyImportView<TApprovalPromptViewModel>(Window window, TApprovalPromptViewModel viewModel) where TApprovalPromptViewModel : GamePropertyImportViewModel, new()
    {
        throw new NotImplementedException();
        //return new GamePropertyImportView(window) { DataContext = viewModel };
    }

    public virtual void ShowProgress(Action<GlobalProgressActionArgs> action, GlobalProgressOptions progressOptions) => playniteApi.Dialogs.ActivateGlobalProgress(action, progressOptions);
    public virtual void AddNotification(string id, string text, NotificationType type) => playniteApi.Notifications.Add(id, text, type);
    public virtual void ShowDialog(string bodyText, string caption, MessageBoxButton button, MessageBoxImage icon) => playniteApi.Dialogs.ShowMessage(bodyText, caption, button, icon);
    public bool GameIsInCurrentFilter(Game game) => playniteApi.MainView.FilteredGames.Contains(game);
}
