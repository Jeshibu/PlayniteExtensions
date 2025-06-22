using Playnite.SDK;

namespace BigFishMetadata;

public class BigFishMetadataSettingsViewModel : PluginSettingsViewModel<BigFishMetadataSettings, BigFishMetadata>
{
    public BigFishMetadataSettingsViewModel(BigFishMetadata plugin, IPlayniteAPI playniteAPI) : base(plugin, playniteAPI)
    {
        // Load saved settings.
        var savedSettings = plugin.LoadPluginSettings<BigFishMetadataSettings>();

        // LoadPluginSettings returns null if no saved data is available.
        if (savedSettings != null)
        {
            Settings = savedSettings;
        }
        else
        {
            Settings = new BigFishMetadataSettings();
        }
    }
}