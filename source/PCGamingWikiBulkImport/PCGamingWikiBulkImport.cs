using PCGamingWikiBulkImport.DataCollection;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UserControl = System.Windows.Controls.UserControl;

namespace PCGamingWikiBulkImport
{
    public class PCGamingWikiBulkImport : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private PCGamingWikiBulkImportSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("40d34387-2436-4b31-951a-36cf2715ee77");

        public PCGamingWikiBulkImport(IPlayniteAPI api) : base(api)
        {
            settings = new PCGamingWikiBulkImportSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            //if (!Settings.Settings.ShowTopPanelButton)
            //    yield break;

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "icon.png");
            yield return new TopPanelItem()
            {
                Icon = iconPath,
                Visible = true,
                Title = "Import PCGamingWiki game property",
                Activated = ImportGameProperty
            };
        }

        public void ImportGameProperty()
        {
            var platformUtility = new PlatformUtility(PlayniteApi);
            var idUtility = new AggregateExternalDatabaseUtility(ExternalDatabase.PCGamingWiki, ExternalDatabase.Steam, ExternalDatabase.GOG);
            var searchProvider = new PCGamingWikiPropertySearchProvider(new CargoQuery(), platformUtility);
            var extra = new PCGamingWikiBulkGamePropertyAssigner(PlayniteApi, idUtility, searchProvider, platformUtility, maxDegreeOfParallelism: 8);
            extra.ImportGameProperty();
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                MenuSection = "PCGamingWiki",
                Description = "Import PCGamingWiki property",
                Action = _ => ImportGameProperty(),
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new PCGamingWikiBulkImportSettingsView();
        }
    }
}