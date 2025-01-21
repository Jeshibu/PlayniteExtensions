using PCGamingWikiBulkImport.DataCollection;
using PCGamingWikiBulkImport.Models;
using PCGamingWikiBulkImport.Views;
using PCGamingWikiMetadata;
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
        private readonly PCGamingWikiMetadataSettings settings;
        private readonly PCGamingWikiPropertySearchProvider pcgwDataSource;

        public PCGamingWikiBulkGamePropertyAssigner(IPlayniteAPI playniteAPI, PCGamingWikiMetadataSettings settings, IExternalDatabaseIdUtility databaseIdUtility, PCGamingWikiPropertySearchProvider dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism = 8)
            : base(playniteAPI, dataSource, platformUtility, databaseIdUtility, ExternalDatabase.PCGamingWiki, maxDegreeOfParallelism)
        {
            this.settings = settings;
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

        private static string IdToString(ExternalDatabase db, string id) => $"{db}:{id}";

        protected override PCGamingWikiSelectedValues SelectGameProperty()
        {
            var selectedProperty = base.SelectGameProperty();
            if (selectedProperty == null)
                return null;

            switch (selectedProperty.FieldInfo.FieldType)
            {
                case CargoFieldType.ListOfString:
                    return SelectStringListProperty(selectedProperty);
                case CargoFieldType.String:
                    return SelectStringProperty(selectedProperty);
                default:
                    return null;
            }
        }

        private PCGamingWikiSelectedValues SelectStringListProperty(PCGamingWikiSelectedValues selectedPropertyCategory)
        {
            var selectedValue = (GenericItemOption<ItemCount>)playniteApi.Dialogs.ChooseItemWithSearch(null, query =>
            {
                var counts = pcgwDataSource.GetCounts(selectedPropertyCategory.FieldInfo, query);
                var options = counts.Select(c => new GenericItemOption<ItemCount>(c) { Name = GetItemDisplayName(selectedPropertyCategory.FieldInfo, c) }).ToList<GenericItemOption>();
                return options;
            });
            if (selectedValue == null)
                return null;

            selectedPropertyCategory.SelectedValues.Add(selectedValue.Item.Value);
            return selectedPropertyCategory;
        }

        private string GetItemDisplayName(CargoFieldInfo field, ItemCount itemCount)
        {
            return $"{itemCount.Value.TrimStart(field.PageNamePrefix)} ({itemCount.Count})";
        }

        private PCGamingWikiSelectedValues SelectStringProperty(PCGamingWikiSelectedValues selectedPropertyCategory)
        {
            try
            {
                var counts = pcgwDataSource.GetCounts(selectedPropertyCategory.FieldInfo, null).ToList();
                var items = counts.Select(c => new SelectableStringViewModel
                {
                    Value = c.Value,
                    DisplayName = GetItemDisplayName(selectedPropertyCategory.FieldInfo, c),
                    IsSelected = GetDefaultSelectionStatus(c.Value)
                });
                var vm = new SelectStringsViewModel(selectedPropertyCategory.Name, items);

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

                selectedPropertyCategory.SelectedValues = vm.Items.Where(i => i.IsSelected).Select(i => i.Value).ToList();
                return selectedPropertyCategory;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error selecting values");
                return null;
            }
        }

        private string[] falseValues = new[] { "false", "unknown", "n/a", "hackable" };

        private bool GetDefaultSelectionStatus(string value) => !falseValues.Contains(value, StringComparer.InvariantCultureIgnoreCase);

        protected override PropertyImportSetting GetPropertyImportSetting(PCGamingWikiSelectedValues searchItem, out string name)
        {
            var p = settings.AddTagPrefix ? GetPrefix(searchItem.FieldInfo) : null;
            var n = GetTagName(searchItem);
            name = $"{p} {n}".Trim();
            return new PropertyImportSetting { ImportTarget = searchItem.FieldInfo.PreferredField };
        }

        private string GetTagName(PCGamingWikiSelectedValues searchItem)
        {
            return searchItem.FieldInfo.FieldType == CargoFieldType.String
             ? searchItem.FieldInfo.FieldDisplayName
             : searchItem.SelectedValues.FirstOrDefault().TrimStart(searchItem.FieldInfo.PageNamePrefix);
        }

        private string GetPrefix(CargoFieldInfo fieldInfo)
        {
            if (fieldInfo.Table != CargoTables.GameInfoBoxTableName)
                return null;

            switch (fieldInfo.Field)
            {
                case "Monetization":
                    return settings.TagPrefixMonetization;
                case "Microtransactions":
                    return settings.TagPrefixMicrotransactions;
                case "Pacing":
                    return settings.TagPrefixPacing;
                case "Perspectives":
                    return settings.TagPrefixPerspectives;
                case "Controls":
                    return settings.TagPrefixControls;
                case "Vehicles":
                    return settings.TagPrefixVehicles;
                case "Themes":
                    return settings.TagPrefixThemes;
                case "Engines":
                    return settings.TagPrefixEngines;
                case "Art_styles":
                    return settings.TagPrefixArtStyles;
                default:
                    return null;
            }
        }
    }

    public class PCGamingWikiSelectedValues : IHasName
    {
        public string Name => FieldInfo.FieldDisplayName;
        public CargoFieldInfo FieldInfo { get; set; }
        public List<string> SelectedValues { get; set; } = new List<string>();
    }
}
