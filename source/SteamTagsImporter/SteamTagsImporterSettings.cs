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
    private readonly SteamTagsImporter plugin;
    private bool _limitTagsToFixedAmount = false;
    private int _fixedTagCount = 5;
    private bool _limitTaggingToPcGames = true;
    private bool _automaticallyAddTagsToNewGames = true;
    private bool _useTagPrefix = false;
    private string _tagPrefix = string.Empty;
    private bool _tagDelistedGames = false;
    private string _delistedTagName = "Delisted";
    private DateTime _lastAutomaticTagUpdate = default;
    private string _languageKey = string.Empty;



    public bool LimitTagsToFixedAmount { get { return _limitTagsToFixedAmount; } set { SetValue(ref _limitTagsToFixedAmount, value); } }

    public int FixedTagCount { get { return _fixedTagCount; } set { SetValue(ref _fixedTagCount, value); } }

    public bool LimitTaggingToPcGames { get { return _limitTaggingToPcGames; } set { SetValue(ref _limitTaggingToPcGames, value); } }

    public bool AutomaticallyAddTagsToNewGames { get { return _automaticallyAddTagsToNewGames; } set { SetValue(ref _automaticallyAddTagsToNewGames, value); } }

    public bool UseTagPrefix { get { return _useTagPrefix; } set { SetValue(ref _useTagPrefix, value); } }

    public string TagPrefix { get { return _tagPrefix; } set { SetValue(ref _tagPrefix, value); } }

    public bool TagDelistedGames { get { return _tagDelistedGames; } set { SetValue(ref _tagDelistedGames, value); } }

    public string DelistedTagName { get { return _delistedTagName; } set { SetValue(ref _delistedTagName, value); } }

    public DateTime LastAutomaticTagUpdate { get { return _lastAutomaticTagUpdate; } set { SetValue(ref _lastAutomaticTagUpdate, value); } }

    public string LanguageKey { get { return _languageKey; } set { SetValue(ref _languageKey, value); } }

    public ObservableCollection<string> OkayTags { get; set; } = [];

    public ObservableCollection<string> BlacklistedTags { get; set; } = [];

    public bool OnlyImportGamesWithThisLanguageSupport { get; set; } = false;

    public bool ShowTopPanelButton { get; set; } = true;
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

    private readonly Dictionary<string, string> _languages = new()
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
    public Dictionary<string, string> Languages
    {
        get { return _languages; }
    }

    [DontSerialize]
    public RelayCommand<IList<object>> WhitelistCommand
    {
        get => new((selectedItems) =>
        {
            var selectedStrings = selectedItems.Cast<string>().ToList();
            foreach (string str in selectedStrings)
            {
                Settings.BlacklistedTags.Remove(str);
                Settings.OkayTags.Add(str);
            }
        }, (a) => a?.Count > 0);
    }

    [DontSerialize]
    public RelayCommand<IList<object>> BlacklistCommand
    {
        get => new((selectedItems) =>
        {
            var selectedStrings = selectedItems.Cast<string>().ToList();
            foreach (string str in selectedStrings)
            {
                Settings.OkayTags.Remove(str);
                Settings.BlacklistedTags.Add(str);
            }
        }, (a) => a?.Count > 0);
    }
}