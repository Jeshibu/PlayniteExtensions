using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SteamTagsImporter;

public class SteamTagsImporterSettings : BulkImportPluginSettings
{
    public bool LimitTagsToFixedAmount { get; set => SetValue(ref field, value); } = false;
    public int FixedTagCount { get; set => SetValue(ref field, value); } = 5;
    public bool LimitTaggingToPcGames { get; set => SetValue(ref field, value); } = true;
    public bool AutomaticallyAddTagsToNewGames { get; set => SetValue(ref field, value); } = true;
    public bool UseTagPrefix { get; set => SetValue(ref field, value); } = false;
    public string TagPrefix { get; set => SetValue(ref field, value); } = string.Empty;
    public bool TagDelistedGames { get; set => SetValue(ref field, value); } = false;
    public string DelistedTagName { get; set => SetValue(ref field, value); } = "Delisted";
    public DateTime LastAutomaticTagUpdate { get; set => SetValue(ref field, value); } = default;
    public string LanguageKey { get; set => SetValue(ref field, value); } = string.Empty;
    public ObservableCollection<string> OkayTags { get; set; } = [];
    public ObservableCollection<string> BlacklistedTags { get; set; } = [];
    public bool OnlyImportGamesWithThisLanguageSupport { get; set; } = false;
}

public class SteamTagsImporterSettingsViewModel : PluginSettingsViewModel<SteamTagsImporterSettings, SteamTagsImporter>
{
    public SteamTagsImporterSettingsViewModel(SteamTagsImporter plugin) : base(plugin, plugin.PlayniteApi)
    {
        // Load saved settings.
        Settings = LoadSavedSettings() ?? new SteamTagsImporterSettings
        {
            LastAutomaticTagUpdate = DateTime.Now,
            MaxDegreeOfParallelism = BulkImportPluginSettings.GetDefaultMaxDegreeOfParallelism()
        };

        Settings.OkayTags = new(Settings.OkayTags.OrderBy(a => a));
        Settings.BlacklistedTags = new(Settings.BlacklistedTags.OrderBy(a => a));
    }

    [DontSerialize]
    public Dictionary<string, string> Languages { get; } = new()
    {
        {string.Empty,"Don't specify (results are region dependent)"},
        {"schinese","简体中文 (Simplified Chinese)"},
        {"tchinese","繁體中文 (Traditional Chinese)"},
        {"japanese","日本語 (Japanese)"},
        {"koreana","한국어 (Korean)"},
        {"thai","ไทย (Thai)"},
        {"bulgarian","Български (Bulgarian)"},
        {"czech","Čeština (Czech)"},
        {"danish","Dansk (Danish)"},
        {"german","Deutsch (German)"},
        {"english","English"},
        {"spanish","Español - España (Spanish - Spain)"},
        {"latam","Español - Latinoamérica (Spanish - Latin America)"},
        {"greek","Ελληνικά (Greek)"},
        {"french","Français (French)"},
        {"italian","Italiano (Italian)"},
        {"indonesian", "Bahasa Indonesia (Indonesian)"},
        {"hungarian","Magyar (Hungarian)"},
        {"dutch","Nederlands (Dutch)"},
        {"norwegian","Norsk (Norwegian)"},
        {"polish","Polski (Polish)"},
        {"portuguese","Português (Portuguese - Portugal)"},
        {"brazilian","Português - Brasil (Portuguese - Brazil)"},
        {"romanian","Română (Romanian)"},
        {"russian","Русский (Russian)"},
        {"finnish","Suomi (Finnish)"},
        {"swedish","Svenska (Swedish)"},
        {"turkish","Türkçe (Turkish)"},
        {"vietnamese","Tiếng Việt (Vietnamese)"},
        {"ukrainian","Українська (Ukrainian)"},
    };

    [DontSerialize]
    public RelayCommand<IList<object>> WhitelistCommand =>
        new((selectedItems) =>
        {
            var selectedStrings = selectedItems.Cast<string>().ToList();
            foreach (string str in selectedStrings)
            {
                Settings.BlacklistedTags.Remove(str);
                Settings.OkayTags.Add(str);
            }
        }, (a) => a?.Count > 0);

    [DontSerialize]
    public RelayCommand<IList<object>> BlacklistCommand =>
        new((selectedItems) =>
        {
            var selectedStrings = selectedItems.Cast<string>().ToList();
            foreach (string str in selectedStrings)
            {
                Settings.OkayTags.Remove(str);
                Settings.BlacklistedTags.Add(str);
            }
        }, (a) => a?.Count > 0);
}
