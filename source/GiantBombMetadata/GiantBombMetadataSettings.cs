using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace GiantBombMetadata;

public class GiantBombMetadataSettings : BulkImportPluginSettings
{
    public string ApiKey { get; set => SetValue(ref field, value?.Trim()); }
    public PropertyImportSetting Characters { get; set; } = new() { Prefix = "Character: ", ImportTarget = PropertyImportTarget.Ignore };
    public PropertyImportSetting Concepts { get; set; } = new() { Prefix = "", ImportTarget = PropertyImportTarget.Tags };
    public PropertyImportSetting Locations { get; set; } = new() { Prefix = "Location: ", ImportTarget = PropertyImportTarget.Tags };
    public PropertyImportSetting Objects { get; set; } = new() { Prefix = "Object: ", ImportTarget = PropertyImportTarget.Ignore };
    public PropertyImportSetting Themes { get; set; } = new() { Prefix = "", ImportTarget = PropertyImportTarget.Tags };
    public PropertyImportSetting People { get; set; } = new() { Prefix = "Person: ", ImportTarget = PropertyImportTarget.Ignore };
    public PropertyImportSetting Franchises { get; set; } = new() { Prefix = "", ImportTarget = PropertyImportTarget.Series };
    public PropertyImportSetting Genres { get; set; } = new() { Prefix = "", ImportTarget = PropertyImportTarget.Genres };
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
        var savedSettings = LoadSavedSettings();

        // LoadSavedSettings returns null if no saved data is available.
        if (savedSettings != null)
        {
            Settings = savedSettings;
            UpgradeSettings();
        }
        else
        {
            Settings = new() { Version = 1 };
        }
    }

    public RelayCommand<object> GetApiKeyCommand => new(_ => { Process.Start(@"https://www.giantbomb.com/api/"); });

    public PropertyImportTarget[] ImportTargets { get; } =
    [
        PropertyImportTarget.Ignore,
        PropertyImportTarget.Genres,
        PropertyImportTarget.Tags,
    ];

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
