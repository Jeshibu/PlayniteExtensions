using Barnite.Scrapers;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
            _scraperManager.Add<PriceChartingScraper>();
            _scraperManager.Add<UpcItemDbScraper>();
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
        /*
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            return base.GetMainMenuItems(args);
        }
        */

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

            string barcode = Regex.Replace(inputResult.SelectedString, @"\s+", string.Empty);

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
                            PlayniteApi.MainView.SelectGame(game.Id);
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
}