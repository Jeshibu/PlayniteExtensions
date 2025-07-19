using PCGamingWikiBulkImport.DataCollection;
using PCGamingWikiBulkImport.Models;
using PCGamingWikiBulkImport.Views;
using PCGamingWikiMetadata;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PCGamingWikiBulkImport;

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

        return selectedProperty.FieldInfo.FieldType switch
        {
            CargoFieldType.ListOfString => SelectStringListProperty(selectedProperty),
            CargoFieldType.String => SelectStringProperty(selectedProperty),
            _ => null,
        };
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

    protected override IEnumerable<PotentialLink> GetPotentialLinks(PCGamingWikiSelectedValues searchItem)
    {
        yield return new PotentialLink(MetadataProviderName, gameDetails => gameDetails.Url, ContainsUrl);
    }

    private bool ContainsUrl(IEnumerable<Link> links, string url)
    {
        if (links == null)
            return false;

        var gdId = DatabaseIdUtility.GetIdFromUrl(url).Id;

        foreach (var link in links)
        {
            var gId = DatabaseIdUtility.GetIdFromUrl(link.Url);
            if (gId.Database != ExternalDatabase.PCGamingWiki)
                continue;

            if(string.Equals(gdId, gId.Id, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }
        return false;
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

        return fieldInfo.Field switch
        {
            "Monetization" => settings.TagPrefixMonetization,
            "Microtransactions" => settings.TagPrefixMicrotransactions,
            "Pacing" => settings.TagPrefixPacing,
            "Perspectives" => settings.TagPrefixPerspectives,
            "Controls" => settings.TagPrefixControls,
            "Vehicles" => settings.TagPrefixVehicles,
            "Themes" => settings.TagPrefixThemes,
            "Engines" => settings.TagPrefixEngines,
            "Art_styles" => settings.TagPrefixArtStyles,
            _ => null,
        };
    }
}

public class PCGamingWikiSelectedValues : IHasName
{
    public string Name => FieldInfo.FieldDisplayName;
    public CargoFieldInfo FieldInfo { get; set; }
    public List<string> SelectedValues { get; set; } = new List<string>();
}
