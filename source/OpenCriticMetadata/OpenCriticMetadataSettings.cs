using Playnite.SDK;
using System.Collections.Generic;

namespace OpenCriticMetadata;

public class OpenCriticMetadataSettings : ObservableObject
{
}

public class OpenCriticMetadataSettingsViewModel : PluginSettingsViewModel<OpenCriticMetadataSettings, OpenCriticMetadata>
{
    public OpenCriticMetadataSettingsViewModel(OpenCriticMetadata plugin) : base(plugin, plugin.PlayniteApi)
    {
        Settings = plugin.LoadPluginSettings<OpenCriticMetadataSettings>() ?? new OpenCriticMetadataSettings();
    }
}
