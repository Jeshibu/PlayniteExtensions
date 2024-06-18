using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;

namespace TvTropesMetadata
{
    public class TvTropesMetadataSettings : BulkImportPluginSettings
    {
        public bool ShowTopPanelButton { get; set; } = true;
    }

    public class TvTropesMetadataSettingsViewModel : PluginSettingsViewModel<TvTropesMetadataSettings, TvTropesMetadata>
    {
        public TvTropesMetadataSettingsViewModel(TvTropesMetadata plugin) : base(plugin, plugin.PlayniteApi)
        {
            Settings = plugin.LoadPluginSettings<TvTropesMetadataSettings>();
            if (Settings == null)
                Settings = new TvTropesMetadataSettings() { MaxDegreeOfParallelism = BulkImportPluginSettings.GetDefaultMaxDegreeOfParallelism() };
        }
    }
}