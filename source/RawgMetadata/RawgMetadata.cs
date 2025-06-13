using Playnite.SDK;
using Playnite.SDK.Plugins;
using Rawg.Common;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace RawgMetadata;

public class RawgMetadata : MetadataPlugin
{
    private static readonly ILogger logger = LogManager.GetLogger();

    private RawgMetadataSettingsViewModel settings { get; set; }

    private RawgApiClient rawgApiClient;

    public override Guid Id { get; } = Guid.Parse("07f4f852-bfc8-4937-b189-3a5a308569a6");

    public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
    {
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
    };

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
            PlayniteApi.Notifications.Add(new NotificationMessage("rawg-metadata-no-apikey", "No API key set. Please set it in the RAWG Metadata extension settings.", NotificationType.Error, OpenSettings));
            return null;
        }

        return rawgApiClient ?? (rawgApiClient = new RawgApiClient(settings.Settings.ApiKey));
    }

    private void OpenSettings()
    {
        base.OpenSettingsView();
    }

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
}