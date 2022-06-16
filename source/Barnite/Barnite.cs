using Barnite.Scrapers;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Barnite
{
    public class Barnite : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private ScraperManager _scraperManager;

        private BarniteSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("fdcf35cc-edcd-4fc3-8640-bb037d3349fe");

        public Barnite(IPlayniteAPI api) : base(api)
        {
            settings = new BarniteSettingsViewModel(this, api);
            Properties = new GenericPluginProperties
            {
                HasSettings = true,
            };

            var platformUtility = new PlatformUtility(api);
            var webclient = new WebDownloader();
            _scraperManager = new ScraperManager(platformUtility, webclient);
            _scraperManager.Add<MobyGamesScraper>();
            _scraperManager.Add<PlayAsiaScraper>();
            _scraperManager.Add<OgdbScraper>();
            _scraperManager.Add<BolScraper>();
            _scraperManager.Add<VGCollectScraper>();
            _scraperManager.Add<RFGenerationScraper>();
            _scraperManager.Add<PriceChartingScraper>();
            _scraperManager.Add<UpcItemDbScraper>();
            _scraperManager.Add<RetroplaceScraper>();
            _scraperManager.InitializeScraperSettingsCollection(settings.Settings.Scrapers);

            var searchContext = new BarcodeSearchContext(_scraperManager, PlayniteApi);
            Searches = new List<SearchSupport> { new SearchSupport("barcode", "Search by barcode", searchContext) };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new BarniteSettingsView();
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "icon.png");
            logger.Debug($"Icon path: {iconPath}");
            yield return new TopPanelItem()
            {
                Icon = iconPath,
                Visible = true,
                Title = "Scan barcode",
                Activated = StartBarcodeEntry
            };
        }

        public void StartBarcodeEntry()
        {
            var inputResult = PlayniteApi.Dialogs.SelectString("Enter barcode", "Barnite", string.Empty);
            if (!inputResult.Result || string.IsNullOrWhiteSpace(inputResult.SelectedString))
                return;

            string barcode = InputHelper.SanitizeBarcodeInput(inputResult.SelectedString);

            PlayniteApi.Dialogs.ActivateGlobalProgress((args) =>
            {
                var orderedScrapers = _scraperManager.GetOrderedListFromSettings(settings.Settings.Scrapers);
                args.ProgressMaxValue = orderedScrapers.Count;
                foreach (var scraper in orderedScrapers)
                {
                    if (args.CancelToken.IsCancellationRequested)
                        return;

                    args.Text = $"Searching {scraper.Name} for {barcode}…";
                    try
                    {
                        var data = scraper.GetMetadataFromBarcode(barcode);
                        if (data == null)
                        {
                            logger.Debug($"No game found in {scraper.Name} for {barcode}");
                        }
                        else
                        {
                            logger.Debug($"Game found in {scraper.Name} for {barcode}!");
                            var game = PlayniteApi.Database.ImportGame(data);
                            PlayniteApi.Dialogs.ShowMessage($"Added {data.Name} via {scraper.Name}!\r\nIt's recommended to use metadata plugins to get more data on the game.", "Barnite");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Error while getting metadata from barcode {barcode} with {scraper.Name}");
                        PlayniteApi.Notifications.Add(new NotificationMessage($"barcode_{scraper.Name}_error", $"Error while getting barcode with {scraper.Name}: {ex.Message}", NotificationType.Error));
                    }
                    args.CurrentProgressValue++;
                }
                PlayniteApi.Dialogs.ShowMessage($"No game found for {barcode} in {orderedScrapers.Count} database(s)", "Barnite");
            }, new GlobalProgressOptions("Searching barcode databases…") { Cancelable = true, IsIndeterminate = false });
        }
    }

    public class InputHelper
    {
        public static string SanitizeBarcodeInput(string input)
        {
            string barcode = Regex.Replace(input, @"\s+", string.Empty);
            return barcode;
        }
    }

    public class BarcodeSearchContext : SearchContext
    {
        public BarcodeSearchContext(ScraperManager scraperManager, IPlayniteAPI playniteAPI)
        {
            ScraperManager = scraperManager;
            PlayniteAPI = playniteAPI;
            Delay = 300;
        }

        public ScraperManager ScraperManager { get; }
        public IPlayniteAPI PlayniteAPI { get; }
        private ILogger logger = LogManager.GetLogger();

        public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args)
        {
            if (!CouldBeBarcode(args.SearchTerm))
            {
                logger.Debug($"Skipped '{args.SearchTerm}' because it doesn't look like a barcode");
                yield break;
            }

            string potentialBarcode = InputHelper.SanitizeBarcodeInput(args.SearchTerm);
            logger.Debug($"Searching for barcode: {potentialBarcode}");

            var tasks = new List<ScraperTaskInfo>();

            foreach (var scraper in ScraperManager.Scrapers)
            {
                tasks.Add(new ScraperTaskInfo(scraper, potentialBarcode));
            }

            foreach (var task in tasks)
            {
                logger.Debug($"Awaiting {task.Scraper.Name} result for {task.Barcode}");
                task.ScrapeTask.Wait(args.CancelToken);
                logger.Debug($"Wait for {task.Scraper.Name} result for {task.Barcode} completed");
                if (task.ScrapeTask.Status != TaskStatus.RanToCompletion)
                {
                    logger.Debug($"Unexpected task status: {task.ScrapeTask.Status}");
                    continue;
                }

                var gameMetadata = task.ScrapeTask.Result;
                if (gameMetadata != null)
                {
                    var action = new SearchItemAction("Add to library", () => AddGameToLibrary(gameMetadata, task.Scraper)) { CloseSearch = true };
                    var searchItem = new SearchItem(gameMetadata.Name, action) { Description = $"via {task.Scraper.Name}" };

                    var gameCoverPath = gameMetadata.CoverImage?.Path;
                    if (!string.IsNullOrEmpty(gameCoverPath))
                        searchItem.Icon = gameCoverPath;

                    logger.Debug("returning " + searchItem.Name);
                    yield return searchItem;
                }

                logger.Debug($"Disposing task to get game metadata from {task.Scraper.Name} for barcode {task.Barcode}");
                task.ScrapeTask.Dispose();
            }
        }

        private class ScraperTaskInfo
        {
            public ScraperTaskInfo(MetadataScraper scraper, string barcode)
            {
                Scraper = scraper;
                Barcode = barcode;
                ScrapeTask = Task.Run(() =>
                {
                    GameMetadata gameMetadata = scraper.GetMetadataFromBarcode(barcode);
                    logger.Debug($"{scraper.Name} result for {barcode}: {gameMetadata?.Name}");
                    return gameMetadata;
                });
            }

            public MetadataScraper Scraper { get; }
            public string Barcode { get; }
            public Task<GameMetadata> ScrapeTask { get; }
            private ILogger logger = LogManager.GetLogger();
        }

        private void AddGameToLibrary(GameMetadata metadata, MetadataScraper scraper)
        {
            var game = PlayniteAPI.Database.ImportGame(metadata);
            //PlayniteAPI.Dialogs.ShowMessage($"Added {game.Name} via {scraper.Name}!\r\nIt's recommended to use metadata plugins to get more data on the game.", "Barnite");
            //PlayniteAPI.MainView.SelectGame(game.Id);
        }

        private static bool CouldBeBarcode(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;

            return str.Count(c => !char.IsWhiteSpace(c)) > 8
                && str.Any(c => char.IsNumber(c));
        }
    }
}