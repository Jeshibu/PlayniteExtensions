using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace BigFishMetadata;

// ReSharper disable ClassNeverInstantiated.Global
public class BigFishMetadata : MetadataPlugin
{
    private readonly IWebDownloader downloader = new WebDownloader();
    private readonly IPlatformUtility platformUtility;

    private BigFishMetadataSettingsViewModel settings { get; set; }

    public override Guid Id { get; } = Guid.Parse("dd66a036-f197-4db0-b274-20b253f7ae08");

    public override List<MetadataField> SupportedFields =>
    [
        MetadataField.Name,
        MetadataField.Description,
        MetadataField.Genres,
        MetadataField.Developers,
        MetadataField.ReleaseDate,
        MetadataField.InstallSize,
        MetadataField.CoverImage,
        MetadataField.BackgroundImage,
        MetadataField.CommunityScore,
        MetadataField.Links,
    ];

    public override string Name { get; } = "Big Fish";

    public BigFishMetadata(IPlayniteAPI api) : base(api)
    {
        settings = new BigFishMetadataSettingsViewModel(this, api);
        Properties = new MetadataPluginProperties { HasSettings = true };
        platformUtility = new PlatformUtility(api);
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        var searchProvider = new BigFishSearchProvider(downloader, settings.Settings);
        return new BigFishMetadataProvider(searchProvider, options, this, platformUtility);
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new BigFishMetadataSettingsView();
    }
}
