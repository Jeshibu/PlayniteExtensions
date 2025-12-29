using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using TvTropesMetadata.SearchProviders;

namespace TvTropesMetadata;

public class TvTropesMetadata : MetadataPlugin
{
    private static readonly ILogger logger = LogManager.GetLogger();

    private TvTropesMetadataSettingsViewModel settings { get; set; }

    public override Guid Id { get; } = Guid.Parse("6b53e885-f8dc-4028-a11d-807f891018ae");

    public override List<MetadataField> SupportedFields => TvTropesMetadataProvider.Fields;

    public override string Name { get; } = "TV Tropes";

    public TvTropesMetadata(IPlayniteAPI api) : base(api)
    {
        settings = new TvTropesMetadataSettingsViewModel(this);
        Properties = new MetadataPluginProperties
        {
            HasSettings = true
        };
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        var searchProvider = new WorkSearchProvider(new Scraping.WorkScraper(PlayniteApi.WebViews), settings.Settings);
        return new TvTropesMetadataProvider(searchProvider, options, PlayniteApi, new PlatformUtility(PlayniteApi));
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new TvTropesMetadataSettingsView();
    }

    public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
    {
        if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            yield return new() { Description = "Import Trope", MenuSection = "@TV Tropes", Action = _ => ImportGameProperty() };
    }

    public override IEnumerable<TopPanelItem> GetTopPanelItems()
    {
        if (!settings.Settings.ShowTopPanelButton)
            yield break;

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "icon.png");
        yield return new()
        {
            Icon = iconPath,
            Visible = true,
            Title = "Import TV Trope",
            Activated = ImportGameProperty
        };
    }

    private void ImportGameProperty()
    {
        var searchProvider = new TropeSearchProvider(new Scraping.TropeScraper(PlayniteApi.WebViews), settings.Settings);
        var extra = new BulkTropeAssigner(PlayniteApi, searchProvider, new PlatformUtility(PlayniteApi), settings.Settings);
        extra.ImportGameProperty();
    }
}
