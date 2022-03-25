using Barnite.Scrapers;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Barnite
{
    public class Barnite : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private List<MetadataScraper> Scrapers = new List<MetadataScraper>();

        //private BarniteSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("fdcf35cc-edcd-4fc3-8640-bb037d3349fe");

        public Barnite(IPlayniteAPI api) : base(api)
        {
            //settings = new BarniteSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = false
            };

            var platformUtility = new PlatformUtility(api);
            Scrapers.Add(new MobyGamesScraper(platformUtility));
            Scrapers.Add(new PriceChartingScraper(platformUtility));
        }

        /*
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new BarniteSettingsView();
        }

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
                args.ProgressMaxValue = Scrapers.Count;
                foreach (var scraper in Scrapers)
                {
                    args.Text = $"Searching {scraper.Name} for {barcode}...";
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
                            PlayniteApi.Database.ImportGame(data);
                            PlayniteApi.Dialogs.ShowMessage($"Added {data.Name}!", "Barnite");
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
                PlayniteApi.Dialogs.ShowMessage($"No game found for {barcode} in {Scrapers.Count} databases", "Barnite");
            }, new GlobalProgressOptions("Searching barcode databases...") { Cancelable = true, IsIndeterminate = false });
        }
    }
}