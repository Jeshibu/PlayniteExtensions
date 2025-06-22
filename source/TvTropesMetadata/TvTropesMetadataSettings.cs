using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;

namespace TvTropesMetadata;

public class TvTropesMetadataSettings : BulkImportPluginSettings
{
    public bool ShowTopPanelButton { get; set; }
    public string TropePrefix { get; set; }
    public bool OnlyFirstGamePerTropeListItem { get; set; }
}

public class TvTropesMetadataSettingsViewModel : PluginSettingsViewModel<TvTropesMetadataSettings, TvTropesMetadata>
{
    public TvTropesMetadataSettingsViewModel(TvTropesMetadata plugin) : base(plugin, plugin.PlayniteApi)
    {
        Settings = plugin.LoadPluginSettings<TvTropesMetadataSettings>();
        if (Settings == null)
        {
            Settings = new TvTropesMetadataSettings()
            {
                MaxDegreeOfParallelism = BulkImportPluginSettings.GetDefaultMaxDegreeOfParallelism(),
                TropePrefix = "Trope: ",
                ShowTopPanelButton = true,
                OnlyFirstGamePerTropeListItem = true,
            };
        }
    }
}