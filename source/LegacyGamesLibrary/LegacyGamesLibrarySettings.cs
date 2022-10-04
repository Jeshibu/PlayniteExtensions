using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyGamesLibrary
{
    public class LegacyGamesLibrarySettings : ObservableObject
    {
        private bool useCovers = false;

        public bool UseCovers { get => useCovers; set => SetValue(ref useCovers, value); }
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
}