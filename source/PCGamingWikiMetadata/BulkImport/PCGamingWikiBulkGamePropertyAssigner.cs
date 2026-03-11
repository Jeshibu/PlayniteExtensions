using PCGamingWikiBulkImport;
using PCGamingWikiBulkImport.DataCollection;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PCGamingWikiMetadata.BulkImport;

internal class PCGamingWikiBulkGamePropertyAssigner(
    IPlayniteAPI playniteApi,
    PCGamingWikiMetadataSettings settings,
    IExternalDatabaseIdUtility databaseIdUtility,
    PCGamingWikiPropertySearchProvider dataSource,
    IPlatformUtility platformUtility,
    int maxDegreeOfParallelism = 8
) : BulkGamePropertyAssigner<PCGamingWikiSelectedValues, GamePropertyImportViewModel>(
    playniteApi.Database,
    new PCGamingWikiBulkImportUserInterface(playniteApi),
    dataSource,
    platformUtility,
    databaseIdUtility,
    ExternalDatabase.PCGamingWiki,
    maxDegreeOfParallelism
)
{
    private PCGamingWikiBulkImportUserInterface PcgwUi => (PCGamingWikiBulkImportUserInterface)Ui;

    public override string MetadataProviderName => "PCGamingWiki";

    protected override string GetGameIdFromUrl(string url)
    {
        var idMatch = DatabaseIdUtility.GetIdFromUrl(url);
        return idMatch.Database switch
        {
            ExternalDatabase.None => null,
            _ => IdToString(idMatch.Database, idMatch.Id)
        };
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
        var selectedItem = Ui.ChooseItemWithSearch<ItemCount>(null, query =>
        {
            var counts = dataSource.GetCounts(selectedPropertyCategory.FieldInfo, query);
            var options = counts.Select(c => new GenericItemOption<ItemCount>(c) { Name = GetItemDisplayName(selectedPropertyCategory.FieldInfo, c) }).ToList<GenericItemOption>();
            return options;
        });
        if (selectedItem == null)
            return null;

        selectedPropertyCategory.SelectedValues.Add(selectedItem.Value);
        return selectedPropertyCategory;
    }

    private static string GetItemDisplayName(CargoFieldInfo field, ItemCount itemCount) => $"{itemCount.Value.TrimStart(field.PageNamePrefix)} ({itemCount.Count})";

    private PCGamingWikiSelectedValues SelectStringProperty(PCGamingWikiSelectedValues selectedPropertyCategory)
    {
        try
        {
            var counts = dataSource.GetCounts(selectedPropertyCategory.FieldInfo, null).ToList();
            var items = counts.Select(c => new SelectableStringViewModel
            {
                Value = c.Value,
                DisplayName = GetItemDisplayName(selectedPropertyCategory.FieldInfo, c),
                IsSelected = GetDefaultSelectionStatus(c.Value)
            });
            var vm = new SelectStringsViewModel(selectedPropertyCategory.Name, items);

            vm = PcgwUi.SelectString(vm);
            if (vm == null)
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

    protected override IEnumerable<PotentialLink> GetPotentialLinks(PCGamingWikiSelectedValues searchItem) =>
    [
        new(MetadataProviderName, gameDetails => gameDetails.Url, ContainsUrl)
    ];

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

            if (string.Equals(gdId, gId.Id, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }

    private readonly string[] _falseValues = ["false", "unknown", "n/a", "hackable"];

    private bool GetDefaultSelectionStatus(string value) => !_falseValues.Contains(value, StringComparer.InvariantCultureIgnoreCase);

    protected override PropertyImportSetting GetPropertyImportSetting(PCGamingWikiSelectedValues searchItem, out string name)
    {
        var p = settings.AddTagPrefix ? GetPrefix(searchItem.FieldInfo) : null;
        var n = GetTagName(searchItem);
        name = $"{p} {n}".Trim();
        return new() { ImportTarget = searchItem.FieldInfo.PreferredField };
    }

    private static string GetTagName(PCGamingWikiSelectedValues searchItem)
    {
        return searchItem.FieldInfo.FieldType == CargoFieldType.String
            ? searchItem.FieldInfo.FieldDisplayName
            : searchItem.SelectedValues.FirstOrDefault().TrimStart(searchItem.FieldInfo.PageNamePrefix);
    }

    private string GetPrefix(CargoFieldInfo fieldInfo) => fieldInfo.Table switch
    {
        CargoTables.Names.GameInfoBox => fieldInfo.Field switch
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
        },
        _ => fieldInfo.FieldType switch
        {
            CargoFieldType.ListOfString => $"{fieldInfo.FieldDisplayName}:",
            CargoFieldType.String => $"{fieldInfo.TableDisplayName}:",
            _ => null,
        }
    };
}

public class PCGamingWikiSelectedValues : IHasName
{
    public string Name => FieldInfo.FieldDisplayName;
    public CargoFieldInfo FieldInfo { get; set; }
    public List<string> SelectedValues { get; set; } = [];
}
