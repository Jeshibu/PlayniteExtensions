using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Windows;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System.Windows.Controls;
using System;

namespace SteamTagsImporter.BulkImport
{
    public class SteamPropertyBulkImporter : BulkGamePropertyAssigner<SteamProperty, SteamGamePropertyImportViewModel>
    {
        public override string MetadataProviderName => "Steam";
        private readonly SteamTagsImporterSettings settings;

        public SteamPropertyBulkImporter(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<SteamProperty, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, SteamTagsImporterSettings settings)
            : base(playniteAPI, dataSource, platformUtility, settings.MaxDegreeOfParallelism)
        {
            AllowEmptySearchQuery = true;
            this.settings = settings;
        }

        protected override UserControl GetBulkPropertyImportView(Window window, SteamGamePropertyImportViewModel viewModel)
        {
            return new GamePropertyImportView(window) { DataContext = viewModel };
        }

        protected override string GetGameIdFromUrl(string url) => SteamAppIdUtility.GetSteamGameIdFromUrl(url);

        protected override PropertyImportSetting GetPropertyImportSetting(SteamProperty searchItem, out string name)
        {
            name = searchItem.Name;

            var target = GetTarget(searchItem.Param);

            return new PropertyImportSetting
            {
                ImportTarget = target,
                Prefix = (settings.UseTagPrefix && target == PropertyImportTarget.Tags) ? settings.TagPrefix : null
            };
        }

        protected override string GetIdFromGameLibrary(Guid libraryPluginId, string gameId) => libraryPluginId == SteamAppIdUtility.SteamLibraryPluginId ? gameId : null;

        private static PropertyImportTarget GetTarget(string param)
        {
            switch (param)
            {
                case "tags": return PropertyImportTarget.Tags;
                default: return PropertyImportTarget.Features;
            }
        }
    }
}
