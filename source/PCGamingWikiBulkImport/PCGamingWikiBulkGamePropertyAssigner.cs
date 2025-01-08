using PCGamingWikiBulkImport.DataCollection;
using PCGamingWikiBulkImport.Models;
using PCGamingWikiBulkImport.Views;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PCGamingWikiBulkImport
{
    internal class PCGamingWikiBulkGamePropertyAssigner : BulkGamePropertyAssigner<PCGamingWikiSelectedValues, GamePropertyImportViewModel>
    {
        private readonly PCGamingWikiPropertySearchProvider pcgwDataSource;

        public PCGamingWikiBulkGamePropertyAssigner(IPlayniteAPI playniteAPI, IExternalDatabaseIdUtility databaseIdUtility, PCGamingWikiPropertySearchProvider dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism = 8)
            : base(playniteAPI, dataSource, platformUtility, databaseIdUtility, ExternalDatabase.PCGamingWiki, maxDegreeOfParallelism)
        {
            this.pcgwDataSource = dataSource;
            AllowEmptySearchQuery = true;
        }

        public override string MetadataProviderName => "PCGamingWiki";

        protected override string GetGameIdFromUrl(string url)
        {
            var idMatch = DatabaseIdUtility.GetIdFromUrl(url);
            if (idMatch.Database != ExternalDatabase.None)
                return IdToString(idMatch.Database, idMatch.Id);

            return null;
        }

        protected override string GetIdFromGameLibrary(Guid libraryPluginId, string gameId)
        {
            var db = DatabaseIdUtility.GetDatabaseFromPluginId(libraryPluginId);
            if (db == ExternalDatabase.None)
                return null;

            return IdToString(db, gameId);
        }

        private static string IdToString(ExternalDatabase db, string id) => $"{db}:{id}";

        protected override PCGamingWikiSelectedValues SelectGameProperty()
        {
            var selectedProperty = base.SelectGameProperty();
            if (selectedProperty == null)
                return null;

            var counts = pcgwDataSource.GetCounts(selectedProperty.FieldInfo).Where(c => c.Value != null).ToList();

            if (selectedProperty.FieldInfo.HasReferenceTable)
            {
                var options = counts.Select(c => new GenericItemOption(c.Value, null)).ToList();
                var selectedValue = playniteApi.Dialogs.ChooseItemWithSearch(options, query =>
                {
                    if (string.IsNullOrWhiteSpace(query))
                        return options;

                    return options.Where(o => o.Name.Contains(query, StringComparison.InvariantCultureIgnoreCase)).ToList();
                });
                if (selectedValue == null)
                    return null;

                selectedProperty.SelectedValues.Add(selectedValue.Name);
            }
            else
            {
                var items = counts.Select(c => new SelectableStringViewModel
                {
                    Value = c.Value,
                    DisplayName = $"{c.Value} ({c.Count})",
                    IsSelected = GetDefaultSelectionStatus(c.Value)
                });
                var vm = new SelectStringsViewModel(selectedProperty.Name, items);

                var window = playniteApi.Dialogs.CreateWindow(new WindowCreationOptions { ShowCloseButton = true, ShowMaximizeButton = true, ShowMinimizeButton = false });
                var view = new SelectStringsView(window) { DataContext = vm };
                window.Content = view;
                window.SizeToContent = SizeToContent.WidthAndHeight;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.Title = "Select games";
                window.SizeChanged += Window_SizeChanged;
                var dialogResult = window.ShowDialog();
                if (dialogResult != true)
                    return null;

                selectedProperty.SelectedValues = vm.Items.Where(i => i.IsSelected).Select(i => i.Value).ToList();
            }
            return selectedProperty;
        }

        private string[] falseValues = new[] { "false", "unknown", "n/a", "hackable" };

        private bool GetDefaultSelectionStatus(string value) => !falseValues.Contains(value, StringComparer.InvariantCultureIgnoreCase);

        protected override PropertyImportSetting GetPropertyImportSetting(PCGamingWikiSelectedValues searchItem, out string name)
        {
            name = searchItem.FieldInfo.HasReferenceTable
                ? searchItem.SelectedValues.FirstOrDefault()
                : searchItem.FieldInfo.FieldDisplayName;
            return new PropertyImportSetting { ImportTarget = searchItem.FieldInfo.PreferredField };
        }
    }

    public class PCGamingWikiSelectedValues : IHasName
    {
        public string Name => FieldInfo.FieldDisplayName;
        public CargoFieldInfo FieldInfo { get; set; }
        public List<string> SelectedValues { get; set; } = new List<string>();
    }
}
