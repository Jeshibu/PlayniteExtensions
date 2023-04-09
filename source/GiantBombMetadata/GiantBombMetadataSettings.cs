using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GiantBombMetadata
{
    public class GiantBombMetadataSettings : ObservableObject
    {
        public string ApiKey { get; set; }
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
            }
            else
            {
                Settings = new GiantBombMetadataSettings();
            }
        }

        public RelayCommand<object> GetApiKeyCommand
        {
            get => new RelayCommand<object>((a) =>
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

        public Dictionary<MultiValuedPropertySelectionMode, string> PropertySelectionModes { get; } = new Dictionary<MultiValuedPropertySelectionMode, string>
        {
            { MultiValuedPropertySelectionMode.All, "All" },
            { MultiValuedPropertySelectionMode.OnlyShortest, "Only the shortest one" },
            { MultiValuedPropertySelectionMode.OnlyLongest, "Only the longest one" },
        };
    }
}