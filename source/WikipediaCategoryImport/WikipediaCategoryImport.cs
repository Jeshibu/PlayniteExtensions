using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using WikipediaCategoryImport.BulkImport;
using WikipediaCategoryImport.Settings;

namespace WikipediaCategoryImport;

[UsedImplicitly]
public class WikipediaCategoryImport : MetadataPlugin
{
    private readonly WikipediaSettingsViewmodel _settings;

    public WikipediaCategoryImport(IPlayniteAPI playniteApi) : base(playniteApi)
    {
        Properties = new() { HasSettings = false };
        _settings = new(this, playniteApi);
    }

    public override Guid Id { get; } = Guid.Parse("c99fbe35-d8e6-4e75-a579-23f9bcdfd69e");

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        return new WikipediaCategoryMetadataProvider(new WikipediaGameSearchProvider(new(new WebDownloader(), PlayniteApi.ApplicationInfo.ApplicationVersion)), options, PlayniteApi, new PlatformUtility());
    }

    public override string Name => "Wikipedia Categories";
    public override List<MetadataField> SupportedFields { get; } = [MetadataField.Tags];

    public override IEnumerable<TopPanelItem> GetTopPanelItems()
    {
        if (!_settings.Settings.ShowTopPanelButton)
            yield break;

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "icon.png");
        yield return new()
        {
            Icon = iconPath,
            Visible = true,
            Title = "Import Wikipedia category",
            Activated = ImportGameProperty
        };
    }

    public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
    {
        if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            yield return new() { MenuSection = "@Wikipedia", Description = "Import Wikipedia category", Action = _ => ImportGameProperty(), };
    }

    private void ImportGameProperty()
    {
        var api = new WikipediaApi(new WebDownloader(), PlayniteApi.ApplicationInfo.ApplicationVersion);
        var searchProvider = new WikipediaCategorySearchProvider(api);
        var bulk = new WikipediaCategoryBulkImport(PlayniteApi, searchProvider, new PlatformUtility(), _settings.Settings.MaxDegreeOfParallelism);
        bulk.ImportGameProperty();
    }
}
