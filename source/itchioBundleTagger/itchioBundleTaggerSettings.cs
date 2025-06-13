using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace itchioBundleTagger;

public class itchioBundleTaggerSettings
{
    public bool UseTagPrefix { get; set; } = true;

    public string TagPrefix { get; set; } = "[itch.io] ";

    public bool AddAvailableOnSteamTag { get; set; } = true;

    public bool AddFreeTag { get; set; } = false;

    public bool AddSteamLink { get; set; } = true;

    public bool RunOnLibraryUpdate { get; set; } = true;

    public bool ShowInContextMenu { get; set; } = false;

    public Dictionary<string, Guid> TagIds { get; set; } = new Dictionary<string, Guid>();

    public List<BundleCheckbox> BundleSettings { get; set; } = new List<BundleCheckbox>();

    public int SettingsVersion = 0;
}

public class itchioBundleTaggerSettingsViewModel : PluginSettingsViewModel<itchioBundleTaggerSettings, itchioBundleTagger>
{
    public Labels Labels { get; }

    public itchioBundleTaggerSettingsViewModel(itchioBundleTagger plugin, itchIoTranslator translator) : base(plugin, plugin.PlayniteApi)
    {
        Labels = new Labels(translator);

        // Load saved settings.
        var savedSettings = plugin.LoadPluginSettings<itchioBundleTaggerSettings>();

        // LoadPluginSettings returns null if no saved data is available.
        Settings = savedSettings ?? new itchioBundleTaggerSettings();

        InstantiateCheckboxes();
    }

    private void InstantiateCheckboxes()
    {
        foreach (var bundleTag in Labels.BundleTags)
        {
            var bundleSettings = Settings.BundleSettings.FirstOrDefault(t => t.Key == bundleTag.Key);
            if (bundleSettings != null)
                bundleSettings.Text = bundleTag.Value;
            else
                Settings.BundleSettings.Add(new BundleCheckbox { Key = bundleTag.Key, Text = bundleTag.Value });
        }
    }
}

public class BundleCheckbox
{
    public bool IsChecked { get; set; } = true;
    public string Key { get; set; }

    [DontSerialize]
    public string Text { get; set; }
}

public class Labels
{
    public Labels(itchIoTranslator translator)
    {
        Translator = translator;
    }

    public Labels()
    {
        Translator = new itchIoTranslator("en-US");
    }

    private itchIoTranslator Translator { get; }

    public string UseTagPrefix => Translator.TagPrefixSetting;
    public string AddFreeTag => Translator.AddFreeTagSetting;
    public string AddAvailableOnSteamTag => Translator.AddSteamTagSetting;
    public string AddSteamLink => Translator.AddSteamLinkSetting;
    public string RunOnLibraryUpdate => Translator.RunOnLibraryUpdate;
    public string ShowInContextMenu => Translator.ShowInContextMenu;
    public string AddBundleTagsHeader => Translator.AddBundleTagsHeader;

    public Dictionary<string, string> BundleTags => Translator.GetBundleTags();
}