using Playnite.SDK;
using Rawg.Common;
using System.Diagnostics;

namespace RawgMetadata;

public class RawgMetadataSettings : RawgBaseSettings
{
}

public class RawgMetadataSettingsViewModel : PluginSettingsViewModel<RawgMetadataSettings, RawgMetadata>
{

    public RawgMetadataSettingsViewModel(RawgMetadata plugin) : base(plugin, plugin.PlayniteApi)
    {
        // Load saved settings.
        var savedSettings = plugin.LoadPluginSettings<RawgMetadataSettings>();

        // LoadPluginSettings returns null if no saved data is available.
        if (savedSettings != null)
        {
            Settings = savedSettings;
        }
        else
        {
            Settings = new RawgMetadataSettings();
        }
    }

    public RelayCommand<object> LoginCommand
    {
        get => new RelayCommand<object>((a) =>
        {
            Process.Start(@"https://rawg.io/login?forward=developer");
        });
    }

    public RelayCommand<object> LanguageCodesReferenceCommand
    {
        get => new RelayCommand<object>((a) =>
        {
            Process.Start(@"https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes");
        });
    }
}