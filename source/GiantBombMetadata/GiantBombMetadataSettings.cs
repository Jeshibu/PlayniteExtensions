﻿using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace GiantBombMetadata;

public class GiantBombMetadataSettings : BulkImportPluginSettings
{
    private string apiKey;

    public string ApiKey
    {
        get { return apiKey; }
        set
        {
            apiKey = value?.Trim();
            OnPropertyChanged(nameof(ApiKey));
        }
    }
    public PropertyImportSetting Characters { get; set; } = new PropertyImportSetting { Prefix = "Character: ", ImportTarget = PropertyImportTarget.Ignore };
    public PropertyImportSetting Concepts { get; set; } = new PropertyImportSetting { Prefix = "", ImportTarget = PropertyImportTarget.Tags };
    public PropertyImportSetting Locations { get; set; } = new PropertyImportSetting { Prefix = "Location: ", ImportTarget = PropertyImportTarget.Tags };
    public PropertyImportSetting Objects { get; set; } = new PropertyImportSetting { Prefix = "Object: ", ImportTarget = PropertyImportTarget.Ignore };
    public PropertyImportSetting Themes { get; set; } = new PropertyImportSetting { Prefix = "", ImportTarget = PropertyImportTarget.Tags };
    public PropertyImportSetting People { get; set; } = new PropertyImportSetting { Prefix = "Person: ", ImportTarget = PropertyImportTarget.Ignore };
    public MultiValuedPropertySelectionMode FranchiseSelectionMode { get; set; } = MultiValuedPropertySelectionMode.All;
    public bool ShowTopPanelButton { get; set; } = true;
}

public enum MultiValuedPropertySelectionMode
{
    All,
    OnlyShortest,
    OnlyLongest,
}

public class GiantBombMetadataSettingsViewModel : PluginSettingsViewModel<GiantBombMetadataSettings, GiantBombMetadata>
{
    public GiantBombMetadataSettingsViewModel(GiantBombMetadata plugin) : base(plugin, plugin.PlayniteApi)
    {
        // Load saved settings.
        var savedSettings = plugin.LoadPluginSettings<GiantBombMetadataSettings>();

        // LoadPluginSettings returns null if no saved data is available.
        if (savedSettings != null)
        {
            Settings = savedSettings;
            UpgradeSettings();
        }
        else
        {
            Settings = new GiantBombMetadataSettings { Version = 1 };
        }
    }

    public RelayCommand<object> GetApiKeyCommand
    {
        get => new((a) =>
        {
            Process.Start(@"https://www.giantbomb.com/api/");
        });
    }

    public PropertyImportTarget[] ImportTargets { get; } = new[]
    {
        PropertyImportTarget.Ignore,
        PropertyImportTarget.Genres,
        PropertyImportTarget.Tags,
    };

    public Dictionary<MultiValuedPropertySelectionMode, string> PropertySelectionModes { get; } = new()
    {
        { MultiValuedPropertySelectionMode.All, "All" },
        { MultiValuedPropertySelectionMode.OnlyShortest, "Only the shortest one" },
        { MultiValuedPropertySelectionMode.OnlyLongest, "Only the longest one" },
    };

    private void UpgradeSettings()
    {
        if (Settings.Version < 1)
            Settings.MaxDegreeOfParallelism = GiantBombMetadataSettings.GetDefaultMaxDegreeOfParallelism();

        Settings.Version = 1;
    }
}