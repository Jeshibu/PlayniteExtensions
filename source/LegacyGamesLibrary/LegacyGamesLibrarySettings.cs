using Playnite.SDK;
using System.Collections.Generic;

namespace LegacyGamesLibrary;

public class LegacyGamesLibrarySettings : ObservableObject
{
    public bool UseCovers { get; set; } = false;
    public bool NormalizeGameNames { get; set; } = true;
}

public class LegacyGamesLibrarySettingsViewModel : PluginSettingsViewModel<LegacyGamesLibrarySettings, LegacyGamesLibrary>
{
    public LegacyGamesLibrarySettingsViewModel(LegacyGamesLibrary plugin) : base(plugin, plugin.PlayniteApi)
    {
        // Load saved settings.
        var savedSettings = plugin.LoadPluginSettings<LegacyGamesLibrarySettings>();

        // LoadPluginSettings returns null if not saved data is available.
        if (savedSettings != null)
        {
            Settings = savedSettings;
        }
        else
        {
            Settings = new LegacyGamesLibrarySettings();
        }
    }
}