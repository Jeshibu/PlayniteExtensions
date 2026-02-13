using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace GOGMetadata;

public class GOGMetadata : MetadataPlugin
{
    private readonly ILogger logger = LogManager.GetLogger();
    private readonly IWebDownloader downloader = new WebDownloader();

    private GOGMetadataSettingsViewModel settings { get; set; }

    public override Guid Id { get; } = Guid.Parse("fec6f5ee-4036-47e6-9ade-8e22dd63f93e");

    public static Guid GogLibraryPluginId { get; } = Guid.Parse("AEBE8B7C-6DC3-4A66-AF31-E7375C6B5E9E");

    public override List<MetadataField> SupportedFields => GOGMetadataProvider.Fields;

    public override string Name => "GOG";

    public GOGMetadata(IPlayniteAPI api) : base(api)
    {
        settings = new GOGMetadataSettingsViewModel(this, this.PlayniteApi);
        Properties = new MetadataPluginProperties
        {
            HasSettings = true
        };
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        var platformUtility = new PlatformUtility(PlayniteApi);
        var searchProvider = new GogApiClient(downloader, settings.Settings, platformUtility);
        return new GOGMetadataProvider(searchProvider, options, PlayniteApi, platformUtility);
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        //So for anyone using GongSolutions.Wpf.DragDrop - be aware you have to instantiate something from it before referencing the package in your XAML
        GongSolutions.Wpf.DragDrop.DefaultDragHandler _ = new();
        return new GOGMetadataSettingsView();
    }
}
