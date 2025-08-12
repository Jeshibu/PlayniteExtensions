using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

namespace LaunchBoxMetadata;

public class LaunchBoxMetadata : MetadataPlugin
{
    //So for anyone using GongSolutions.Wpf.DragDrop - be aware you have to instantiate something from it before referencing the package in your XAML
    private readonly GongSolutions.Wpf.DragDrop.DefaultDragHandler dropInfo = new();
    private readonly ILogger logger = LogManager.GetLogger();
    private readonly IPlatformUtility platformUtility;

    private LaunchBoxMetadataSettingsViewModel settings { get; set; }
    private LaunchBoxWebscraper webscraper { get; set; }

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
        settings = new LaunchBoxMetadataSettingsViewModel(this);
        Properties = new MetadataPluginProperties
        {
            HasSettings = true
        };
        platformUtility = new PlatformUtility(PlayniteApi);
        webscraper = new LaunchBoxWebscraper(new WebDownloader());
    }

    public override void OnApplicationStarted(OnApplicationStartedEventArgs args) => CheckConfiguration();

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        if(!CheckConfiguration())
            return null;

        return new LaunchBoxMetadataProvider(options, this, settings.Settings, new LaunchBoxDatabase(GetPluginUserDataPath()), platformUtility, webscraper);
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new LaunchBoxMetadataSettingsView();
    }

    private bool CheckConfiguration()
    {
        var dbPath = LaunchBoxDatabase.GetFilePath(GetPluginUserDataPath());
        if (!File.Exists(dbPath))
        {
            PlayniteApi.Notifications.Add(new NotificationMessage("launchbox-database-missing", "LaunchBox database not initialized. Click here to initialize it.", NotificationType.Error, () => OpenSettingsView()));
            return false;
        }

        if (settings.Settings.DatabaseVersion < LaunchBoxMetadataSettings.CurrentDatabaseVersion)
        {
            PlayniteApi.Notifications.Add(new NotificationMessage("launchbox-database-outdated", "LaunchBox database format outdated. Click here to update it.", NotificationType.Error, () => OpenSettingsView()));
            return false;
        }

        return true;
    }
}