using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using LaunchBoxMetadata.GenreImport;

namespace LaunchBoxMetadata;

public class LaunchBoxMetadata : MetadataPlugin
{
    //So for anyone using GongSolutions.Wpf.DragDrop - be aware you have to instantiate something from it before referencing the package in your XAML
    private readonly GongSolutions.Wpf.DragDrop.DefaultDragHandler dropInfo = new();
    private readonly ILogger logger = LogManager.GetLogger();
    private readonly IPlatformUtility platformUtility;

    private LaunchBoxMetadataSettingsViewModel Settings { get; set; }
    private LaunchBoxWebScraper WebScraper { get; set; }

    public override Guid Id { get; } = Guid.Parse("3b1908f2-de02-48c9-9633-10d978903652");

    public override List<MetadataField> SupportedFields { get; } =
    [
        MetadataField.Name,
        MetadataField.Description,
        MetadataField.Platform,
        MetadataField.CommunityScore,
        MetadataField.ReleaseDate,
        MetadataField.AgeRating,
        MetadataField.Genres,
        MetadataField.Developers,
        MetadataField.Publishers,
        MetadataField.Icon,
        MetadataField.CoverImage,
        MetadataField.BackgroundImage,
        MetadataField.Links,
    ];

    public override string Name => "LaunchBox";

    public LaunchBoxMetadata(IPlayniteAPI api) : base(api)
    {
        Settings = new LaunchBoxMetadataSettingsViewModel(this);
        Properties = new MetadataPluginProperties
        {
            HasSettings = true
        };
        platformUtility = new PlatformUtility(PlayniteApi);
        WebScraper = new LaunchBoxWebScraper(new WebDownloader());
    }

    public override void OnApplicationStarted(OnApplicationStartedEventArgs args) => CheckConfiguration();

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        if (!CheckConfiguration())
            return null;

        return new LaunchBoxMetadataProvider(options, this, Settings.Settings, new LaunchBoxDatabase(GetPluginUserDataPath()), platformUtility, WebScraper);
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return Settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new LaunchBoxMetadataSettingsView();
    }

    public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
    {
        yield return new() { Description = "Import LaunchBox genre", MenuSection = "@LaunchBox", Action = _ => ImportGameProperty() };
    }

    public override IEnumerable<TopPanelItem> GetTopPanelItems()
    {
        if (!Settings.Settings.ShowTopPanelButton)
            yield break;

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "icon.png");
        yield return new TopPanelItem()
        {
            Icon = iconPath,
            Visible = true,
            Title = "Import LaunchBox genre",
            Activated = ImportGameProperty
        };
    }

    private void ImportGameProperty()
    {
        if (!CheckConfiguration())
            return;

        var launchBoxDatabase = new LaunchBoxDatabase(GetPluginUserDataPath());
        var searchProvider = new GenreSearchProvider(launchBoxDatabase, platformUtility);
        var bulkImport = new GenreBulkImport(PlayniteApi, searchProvider, platformUtility, Settings.Settings);
        bulkImport.ImportGameProperty();
    }

    private bool CheckConfiguration()
    {
        var dbPath = LaunchBoxDatabase.GetFilePath(GetPluginUserDataPath());
        if (!File.Exists(dbPath))
        {
            PlayniteApi.Notifications.Add(new("launchbox-database-missing", "LaunchBox database not initialized. Click here to initialize it.", NotificationType.Error, () => OpenSettingsView()));
            return false;
        }

        if (Settings.Settings.DatabaseVersion < LaunchBoxMetadataSettings.CurrentDatabaseVersion)
        {
            PlayniteApi.Notifications.Add(new("launchbox-database-format-outdated", "LaunchBox database format outdated. Click here to update it.", NotificationType.Error, () => OpenSettingsView()));
            return false;
        }

        var dbFile = new FileInfo(dbPath);
        var daysSinceLastUpdate = (DateTime.Now - dbFile.LastWriteTime).TotalDays;
        if (daysSinceLastUpdate > Settings.Settings.AdviseDatabaseUpdateAfterDays)
            PlayniteApi.Notifications.Add(new("launchbox-database-outdated", $"LaunchBox database was last updated {daysSinceLastUpdate:0} days ago. Click here to update it.", NotificationType.Error, () => OpenSettingsView()));

        return true;
    }
}