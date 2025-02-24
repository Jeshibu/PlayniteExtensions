using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace OpenCriticMetadata
{
    public class OpenCriticMetadataSettings : ObservableObject
    {
    }

    public class OpenCriticMetadataSettingsViewModel : PluginSettingsViewModel<OpenCriticMetadataSettings, OpenCriticMetadata>
    {
        private readonly OpenCriticMetadata plugin;
        private OpenCriticMetadataSettings editingClone { get; set; }

        public OpenCriticMetadataSettingsViewModel(OpenCriticMetadata plugin) : base(plugin, plugin.PlayniteApi)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<OpenCriticMetadataSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new OpenCriticMetadataSettings();
            }
        }
    }
}