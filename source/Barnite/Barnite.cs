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
using System.Windows;
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
                HasSettings = true
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
            var inputResult = PlayniteApi.Dialogs.SelectString("Enter a barcode (or many, comma separated)", "Barnite", string.Empty);
            if (!inputResult.Result || string.IsNullOrWhiteSpace(inputResult.SelectedString))
                return;

            //Remove any whitespace and ignore leading/trailing/repeating commas
            string[] barcodes = Regex.Replace(inputResult.SelectedString, @"\s+", string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (barcodes.Length == 0)
                return;

            Dictionary<string, Tuple<Game, string>> barcodeGameDict = new Dictionary<string, Tuple<Game, string>>();
            foreach (var barcode in barcodes)
            {
                barcodeGameDict.Add(barcode, null);
            }
            
            var addedGuids = new List<Guid>();

            PlayniteApi.Dialogs.ActivateGlobalProgress((args) =>
            {
                var orderedScrapers = _scraperManager.GetOrderedListFromSettings(settings.Settings.Scrapers);
                args.ProgressMaxValue = orderedScrapers.Count * barcodes.Length;
                int barcodeCount = 1;
                foreach (var barcode in barcodes)
                {
                    int scraperCount = 0;
                    foreach (var scraper in orderedScrapers)
                    {
                        if (args.CancelToken.IsCancellationRequested)
                            return;

                        args.Text = $"{barcodeCount} of {barcodes.Length}: Searching {scraper.Name} for {barcode}…";
                        scraperCount++;
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
                                addedGuids.Add(game.Id);
                                barcodeGameDict[barcode] = Tuple.Create(game, scraper.Name);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Error while getting metadata from barcode {barcode} with {scraper.Name}");
                            PlayniteApi.Notifications.Add(new NotificationMessage($"barcode_{barcode}_scraper_{scraper.Name}_error", $"Error while getting barcode {barcode} with {scraper.Name}: {ex.Message}", NotificationType.Error));
                        }
                        args.CurrentProgressValue++;
                    }
                    args.CurrentProgressValue += (orderedScrapers.Count - scraperCount);
                    barcodeCount++;
                }
            }, new GlobalProgressOptions("Searching barcode databases…") { Cancelable = true, IsIndeterminate = false });

            PlayniteApi.MainView.SelectGames(addedGuids);
            ShowResults(barcodes, barcodeGameDict);
        }
        private void ShowResults(string[] barcodes, Dictionary<string, Tuple<Game, string>> barcodeGameDict)
        {
            var entries = barcodeGameDict.Select(kvp => new BarcodeEntry
            {
                Barcode = kvp.Key,
                Title = kvp.Value?.Item1?.Name ?? "Not Found",
                Source = kvp.Value?.Item2 ?? "N/A"
            }).ToList();

            PlayniteApi.MainView.UIDispatcher.Invoke(() =>
            {
                var resultsWindow = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions { ShowMinimizeButton = false });
                var view = new BarcodeResultsGrid(resultsWindow, entries);
                resultsWindow.Content = view;
                resultsWindow.Width = 600;
                resultsWindow.Height = 400;
                resultsWindow.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                resultsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                resultsWindow.Title = "Barnite Results";
                resultsWindow.ShowDialog();
            });
        }
    }
}