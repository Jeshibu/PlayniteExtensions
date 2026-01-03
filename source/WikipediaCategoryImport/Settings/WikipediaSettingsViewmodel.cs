using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;

namespace WikipediaCategoryImport.Settings;

public class WikipediaSettings : BulkImportPluginSettings
{
}

public class WikipediaSettingsViewmodel : PluginSettingsViewModel<WikipediaSettings, WikipediaCategoryImport>
{
    public WikipediaSettingsViewmodel(WikipediaCategoryImport plugin, IPlayniteAPI playniteApi) : base(plugin, playniteApi)
    {
        Settings = LoadSavedSettings() ?? new();
    }
}
