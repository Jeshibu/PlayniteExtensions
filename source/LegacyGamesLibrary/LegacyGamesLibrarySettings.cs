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
        Settings = LoadSavedSettings() ?? new();
    }
}