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
        public GiantBombPropertyImportSetting Characters { get; set; } = new GiantBombPropertyImportSetting { Prefix = "Character: ", ImportTarget = PropertyImportTarget.Ignore };
        public GiantBombPropertyImportSetting Concepts { get; set; } = new GiantBombPropertyImportSetting { Prefix = "", ImportTarget = PropertyImportTarget.Tags };
        public GiantBombPropertyImportSetting Locations { get; set; } = new GiantBombPropertyImportSetting { Prefix = "Location: ", ImportTarget = PropertyImportTarget.Tags };
        public GiantBombPropertyImportSetting Objects { get; set; } = new GiantBombPropertyImportSetting { Prefix = "Object: ", ImportTarget = PropertyImportTarget.Ignore };
        public GiantBombPropertyImportSetting Themes { get; set; } = new GiantBombPropertyImportSetting { Prefix = "", ImportTarget = PropertyImportTarget.Tags };
        public GiantBombPropertyImportSetting People { get; set; } = new GiantBombPropertyImportSetting { Prefix = "Person: ", ImportTarget = PropertyImportTarget.Ignore };
        public bool GetSingleSeries { get; set; } = false;
        public bool ShowTopPanelButton { get; set; } = true;
    }

    public class GiantBombPropertyImportSetting
    {
        public string Prefix { get; set; }
        public PropertyImportTarget ImportTarget { get; set; }
    }

    public enum PropertyImportTarget
    {
        Ignore,
        Genres,
        Tags,
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
    }
}