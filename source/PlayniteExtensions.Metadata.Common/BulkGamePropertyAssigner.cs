using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using PlayniteExtensions.Common;
using System.Windows.Controls;

namespace PlayniteExtensions.Metadata.Common
{
    public abstract class BulkGamePropertyAssigner<TSearchItem> where TSearchItem : IHasName
    {
        public BulkGamePropertyAssigner(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<TSearchItem, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility)
        {
            playniteApi = playniteAPI;
            this.dataSource = dataSource;
            this.platformUtility = platformUtility;
        }

        protected readonly ILogger logger = LogManager.GetLogger();
        protected readonly ISearchableDataSourceWithDetails<TSearchItem, IEnumerable<GameDetails>> dataSource;
        private readonly IPlatformUtility platformUtility;
        protected readonly IPlayniteAPI playniteApi;
        public abstract string MetadataProviderName { get; }


        public void ImportGameProperty()
        {
            var selectedItem = SelectGiantBombGameProperty();

            if (selectedItem == null)
                return;

            List<GameDetails> associatedGames = null;
            playniteApi.Dialogs.ActivateGlobalProgress(a =>
            {
                associatedGames = dataSource.GetDetails(selectedItem)?.ToList();
            }, new GlobalProgressOptions("Downloading list of associated games"));

            if (associatedGames == null)
                return;

            var viewModel = PromptGamePropertyImportUserApproval(selectedItem, associatedGames);

            if (viewModel == null)
                return;

            UpdateGames(viewModel);
        }

        private TSearchItem SelectGiantBombGameProperty()
        {
            var selectedItem = playniteApi.Dialogs.ChooseItemWithSearch(null, (a) =>
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

            var addedGameIds = new HashSet<Guid>();
            var matchingGames = new List<GameCheckboxViewModel>();
            var snc = new SortableNameConverter(new string[0], numberLength: 1, removeEditions: true);
            var deflatedNames = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            playniteApi.Dialogs.ActivateGlobalProgress(a =>
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
                    foreach (var g in playniteApi.Database.Games)
                    {
                        if (g.Links != null && gameToMatchId != null)
                        {
                            var ids = g.Links.Select(l => GetGameIdFromUrl(l.Url)).Where(x => x != null).ToList();
                            if (ids.Count > 0)
                            {
                                if (ids.Contains(gameToMatchId) && addedGameIds.Add(g.Id))
                                    matchingGames.Add(new GameCheckboxViewModel(g, gbGame));

                                continue;
                            }
                        }

                        if (!deflatedNames.TryGetValue(g.Name, out string gName))
                        {
                            gName = snc.Convert(g.Name).Deflate();
                            deflatedNames.Add(g.Name, gName);
                        }

                        if (namesToMatch.Any(n => string.Equals(n, gName, StringComparison.InvariantCultureIgnoreCase))
                            && platformUtility.PlatformsOverlap(g.Platforms, gbGame.Platforms)
                            && addedGameIds.Add(g.Id))
                        {
                            matchingGames.Add(new GameCheckboxViewModel(g, gbGame));
                        }
                    }
                }
                matchingGames = matchingGames.OrderBy(g => string.IsNullOrWhiteSpace(g.Game.SortingName) ? g.Game.Name : g.Game.SortingName).ThenBy(g => g.Game.ReleaseDate).ToList();
            }, new GlobalProgressOptions($"Matching {gamesToMatch.Count} games…", true) { IsIndeterminate = false });

            if (matchingGames.Count == 0)
            {
                playniteApi.Dialogs.ShowMessage("No matching games found in your library.", $"{MetadataProviderName} game property assigner", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }

            var viewModel = new GamePropertyImportViewModel() { Name = $"{importSetting.Prefix}{propName}", Games = matchingGames };
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
            var dialogResult = window.ShowDialog();
            if (dialogResult == true)
                return viewModel;
            else
                return null;
        }

        protected abstract UserControl GetBulkPropertyImportView(Window window, GamePropertyImportViewModel viewModel);

        private void UpdateGames(GamePropertyImportViewModel viewModel)
        {
            using (playniteApi.Database.BufferedUpdate())
            {
                DatabaseObject dbItem;
                switch (viewModel.TargetField)
                {
                    case GamePropertyImportTargetField.Category:
                        dbItem = playniteApi.Database.Categories.FirstOrDefault(c => c.Name.Equals(viewModel.Name, StringComparison.InvariantCultureIgnoreCase))
                                 ?? playniteApi.Database.Categories.Add(viewModel.Name);
                        break;
                    case GamePropertyImportTargetField.Genre:
                        dbItem = playniteApi.Database.Genres.FirstOrDefault(c => c.Name.Equals(viewModel.Name, StringComparison.InvariantCultureIgnoreCase))
                                 ?? playniteApi.Database.Genres.Add(viewModel.Name);
                        break;
                    case GamePropertyImportTargetField.Tag:
                        dbItem = playniteApi.Database.Tags.FirstOrDefault(c => c.Name.Equals(viewModel.Name, StringComparison.InvariantCultureIgnoreCase))
                                 ?? playniteApi.Database.Tags.Add(viewModel.Name);
                        break;
                    case GamePropertyImportTargetField.Feature:
                        dbItem = playniteApi.Database.Features.FirstOrDefault(f => f.Name.Equals(viewModel.Name, StringComparison.InvariantCultureIgnoreCase))
                                 ?? playniteApi.Database.Features.Add(viewModel.Name);
                        break;
                    case GamePropertyImportTargetField.Series:
                        dbItem = playniteApi.Database.Series.FirstOrDefault(f => f.Name.Equals(viewModel.Name, StringComparison.InvariantCultureIgnoreCase))
                                 ?? playniteApi.Database.Series.Add(viewModel.Name);
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
                        case GamePropertyImportTargetField.Series:
                            update |= AddItem(g.Game, x => x.SeriesIds, dbItem.Id);
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
                        playniteApi.Database.Games.Update(g.Game);
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
    }
}
