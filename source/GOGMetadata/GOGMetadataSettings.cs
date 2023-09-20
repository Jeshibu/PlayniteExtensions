using Playnite.SDK;
using System.Collections.Generic;

namespace GOGMetadata
{
    public class GOGMetadataSettings : ObservableObject
    {
        public bool UseVerticalCovers { get; set; } = true;
        public string Locale { get; set; } = "en";
    }

    public class GOGMetadataSettingsViewModel : PluginSettingsViewModel<GOGMetadataSettings, GOGMetadata>
    {
        public GOGMetadataSettingsViewModel(GOGMetadata plugin, IPlayniteAPI playniteApi) : base(plugin, playniteApi)
        {
            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<GOGMetadataSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new GOGMetadataSettings();
            }
        }

        public Dictionary<string, string> Languages { get; } = new Dictionary<string, string>
        {
            {"en", "English" },
            {"de", "Deutsch" },
            {"fr", "Français" },
            {"pl", "Polski" },
            {"ru", "Pусский" },
            {"zh", "中文(简体)" },
        };
    }
}