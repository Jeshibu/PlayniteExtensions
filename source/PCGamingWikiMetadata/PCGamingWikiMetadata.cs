using PCGamingWikiBulkImport.DataCollection;
using PCGamingWikiBulkImport;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using System.IO;
using PlayniteExtensions.Common;


namespace PCGamingWikiMetadata;

public class PCGamingWikiMetadata : MetadataPlugin
{
    private static readonly ILogger logger = LogManager.GetLogger();

    private PCGamingWikiMetadataSettingsViewModel settings { get; set; }

    public override Guid Id { get; } = Guid.Parse("c038558e-427b-4551-be4c-be7009ce5a8d");

    public override List<MetadataField> SupportedFields { get; } =
    [
        MetadataField.Name,
        MetadataField.Links,
        MetadataField.ReleaseDate,
        MetadataField.Genres,
        MetadataField.Series,
        MetadataField.Features,
        MetadataField.Developers,
        MetadataField.Publishers,
        MetadataField.CriticScore,
        MetadataField.Tags,
    ];
    public override string Name => "PCGamingWiki";

    public PCGamingWikiMetadata(IPlayniteAPI api) : base(api)
    {
        settings = new PCGamingWikiMetadataSettingsViewModel(this);
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        return new PCGamingWikiMetadataProvider(options, PlayniteApi, settings.Settings);
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new PCGamingWikiMetadataSettingsView();
    }

    public override IEnumerable<TopPanelItem> GetTopPanelItems()
    {
        if (!settings.Settings.ShowTopPanelButton)
            yield break;

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "icon.png");
        yield return new TopPanelItem()
        {
            Icon = iconPath,
            Visible = true,
            Title = "Import PCGamingWiki property",
            Activated = ImportGameProperty
        };
    }

    public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
    {
        yield return new MainMenuItem
        {
            MenuSection = "@PCGamingWiki",
            Description = "Import PCGamingWiki property",
            Action = _ => ImportGameProperty(),
        };
    }
    private void ImportGameProperty()
    {
        var platformUtility = new PlatformUtility(PlayniteApi);
        var idUtility = new AggregateExternalDatabaseUtility(new PCGamingWikiIdUtility(), new SteamIdUtility(), new GOGIdUtility());
        var searchProvider = new PCGamingWikiPropertySearchProvider(new CargoQuery(), platformUtility);
        var bulk = new PCGamingWikiBulkGamePropertyAssigner(PlayniteApi, settings.Settings, idUtility, searchProvider, platformUtility, settings.Settings.MaxDegreeOfParallelism);
        bulk.ImportGameProperty();
    }
}
