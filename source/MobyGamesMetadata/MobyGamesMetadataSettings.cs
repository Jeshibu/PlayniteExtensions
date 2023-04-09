using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobyGamesMetadata
{
    public class MobyGamesMetadataSettings : ObservableObject
    {
        public DataSource DataSource { get; set; } = DataSource.Api;
        public string ApiKey { get; set; } = "moby_GzOVPRacItjN9bYIN69NW79Wbjw";
    }

    [Flags]
    public enum DataSource
    {
        None = 0,
        Api = 1,
        Scraping = 2,
        ApiAndScraping = 3,
    }

    public class MobyGamesMetadataSettingsViewModel : PluginSettingsViewModel<MobyGamesMetadataSettings, MobyGamesMetadata>
    {
        public MobyGamesMetadataSettingsViewModel(MobyGamesMetadata plugin) : base(plugin, plugin.PlayniteApi)
        {
            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<MobyGamesMetadataSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new MobyGamesMetadataSettings();
            }
        }
    }
}