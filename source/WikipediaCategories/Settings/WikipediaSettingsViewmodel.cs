using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;

namespace WikipediaCategories.Settings;

public class WikipediaSettings : BulkImportPluginSettings
{
}

public class WikipediaSettingsViewmodel : PluginSettingsViewModel<WikipediaSettings, WikipediaCategoriesPlugin>
{
    public WikipediaSettingsViewmodel(WikipediaCategoriesPlugin plugin, IPlayniteAPI playniteApi) : base(plugin, playniteApi)
    {
        Settings = LoadSavedSettings() ?? new();
    }
}
