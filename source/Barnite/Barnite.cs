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

            var resultEntries = new List<BarcodeResultEntry>();
            var addedGuids = new List<Guid>();
            var scraperExceptions = new List<String>();

            PlayniteApi.Dialogs.ActivateGlobalProgress((args) =>
            {
                var orderedScrapers = _scraperManager.GetOrderedListFromSettings(settings.Settings.Scrapers);
                args.ProgressMaxValue = orderedScrapers.Count * barcodes.Length;
                int barcodeCount = 1;
                foreach (var barcode in barcodes)
                {
                    int scraperCount = 0;
                    //Assume we didn't find it until it's found
                    BarcodeResultEntry result = new BarcodeResultEntry { 
                        Barcode = barcode,
                        Title = "Not Found", 
                        Source = "N/A",
                        IsSuccessful = false
                    };
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
                                result.Title = game.Name;
                                result.Source = scraper.Name;
                                result.IsSuccessful = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Error while getting metadata from barcode {barcode} with {scraper.Name}: {ex.Message}");
                            scraperExceptions.Add($"Error while getting barcode {barcode} with {scraper.Name}: {ex.Message}");
                        }
                        args.CurrentProgressValue++;
                    }
                    resultEntries.Add(result);
                    args.CurrentProgressValue += (orderedScrapers.Count - scraperCount);
                    barcodeCount++;
                }
            }, new GlobalProgressOptions("Searching barcode databases…") { Cancelable = true, IsIndeterminate = false });

            if(scraperExceptions.Any())
                PlayniteApi.Notifications.Add(new NotificationMessage("barnite_scraper_errors", String.Join(Environment.NewLine, scraperExceptions), NotificationType.Error));
            
            PlayniteApi.MainView.SelectGames(addedGuids);
            ShowResults(resultEntries);
        }
        private void ShowResults(List<BarcodeResultEntry> resultEntries)
        {
            PlayniteApi.MainView.UIDispatcher.Invoke(() =>
            {
                var resultsWindow = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions { ShowMinimizeButton = false });
                var view = new BarcodeResultsGrid(resultsWindow, new BarcodeResultsGridViewModel { ResultEntries = resultEntries });
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