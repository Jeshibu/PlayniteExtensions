using Playnite.SDK;

namespace BigFishMetadata;

public class BigFishMetadataSettingsViewModel : PluginSettingsViewModel<BigFishMetadataSettings, BigFishMetadata>
{
    public BigFishMetadataSettingsViewModel(BigFishMetadata plugin, IPlayniteAPI playniteAPI) : base(plugin, playniteAPI)
    {
        Settings = LoadSavedSettings() ?? new();
    }
}