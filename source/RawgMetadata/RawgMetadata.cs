using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using Rawg.Common;
using RawgMetadata.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace RawgMetadata;

public class RawgMetadata : MetadataPlugin
{
    private static readonly ILogger logger = LogManager.GetLogger();

    private RawgMetadataSettingsViewModel settings { get; set; }

    private RawgApiClient rawgApiClient;

    public override Guid Id { get; } = Guid.Parse("07f4f852-bfc8-4937-b189-3a5a308569a6");

    public override List<MetadataField> SupportedFields { get; } =
    [
        MetadataField.Name,
        MetadataField.ReleaseDate,
        MetadataField.Description,
        MetadataField.CriticScore,
        MetadataField.CommunityScore,
        MetadataField.Platform,
        MetadataField.BackgroundImage,
        MetadataField.Tags,
        MetadataField.Genres,
        MetadataField.Developers,
        MetadataField.Publishers,
        MetadataField.Links
    ];

    public override string Name => "RAWG";

    public RawgMetadata(IPlayniteAPI api) : base(api)
    {
        settings = new RawgMetadataSettingsViewModel(this);
        Properties = new MetadataPluginProperties
        {
            HasSettings = true
        };
    }

    private RawgApiClient GetApiClient()
    {
        if (rawgApiClient != null)
            return rawgApiClient;

        if (string.IsNullOrWhiteSpace(settings.Settings.ApiKey))
        {
            PlayniteApi.Notifications.Add(new("rawg-metadata-no-apikey", "No API key set. Please set it in the RAWG Metadata extension settings.", NotificationType.Error, OpenSettings));
            return null;
        }

        return rawgApiClient ??= new RawgApiClient(settings.Settings.ApiKey);
    }

    private void OpenSettings() => OpenSettingsView();

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        var apiClient = GetApiClient();

        if (apiClient == null)
            return null;

        return new RawgMetadataProvider(options, this, apiClient);
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new RawgMetadataSettingsView();
    }

    public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
    {
        if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            yield return new() { Description = "Import RAWG tag", MenuSection = "@RAWG", Action = _ => ImportGameProperty() };
    }

    public override IEnumerable<TopPanelItem> GetTopPanelItems()
    {
        if (!settings.Settings.ShowTopPanelButton)
            yield break;

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "icon.png");
        yield return new TopPanelItem()
        {
            Icon = iconPath,
            Visible = true,
            Title = "Import RAWG tag",
            Activated = ImportGameProperty
        };
    }

    private void ImportGameProperty()
    {
        var apiClient = GetApiClient();
        if (apiClient == null)
            return;

        var db = new RawgDatabase(GetPluginUserDataPath());
        if (db.GetTagCount() == 0)
        {
            PlayniteApi.Notifications.Add(new("rawg-metadata-no-tags", "No tags in local RAWG database. Please download them in the RAWG Metadata extension settings.", NotificationType.Error, OpenSettings));
            return;
        }

        var dataSource = new RawgTagImportDataSource(db, apiClient, settings.Settings);
        var ui = new BulkPropertyUserInterface(PlayniteApi);
        var platformUtility = new PlatformUtility(PlayniteApi);
        var bulkImport = new RawgTagImport(PlayniteApi.Database, ui, dataSource, platformUtility, new RawgIdUtility(), settings.Settings.MaxDegreeOfParallelism);
        bulkImport.ImportGameProperty();
    }
}
