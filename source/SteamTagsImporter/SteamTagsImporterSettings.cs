using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SteamTagsImporter
{
    public class SteamTagsImporterSettings : ObservableObject, ISettings
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

        public ObservableCollection<string> OkayTags { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<string> BlacklistedTags { get; set; } = new ObservableCollection<string>();

        // Parameterless constructor must exist if you want to use LoadPluginSettings method.
        public SteamTagsImporterSettings()
        {
        }

        public SteamTagsImporterSettings(SteamTagsImporter plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<SteamTagsImporterSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                LimitTagsToFixedAmount = savedSettings.LimitTagsToFixedAmount;
                FixedTagCount = savedSettings.FixedTagCount;
                UseTagPrefix = savedSettings.UseTagPrefix;
                TagPrefix = savedSettings.TagPrefix;
                TagDelistedGames = savedSettings.TagDelistedGames;
                DelistedTagName = savedSettings.DelistedTagName;
                LimitTaggingToPcGames = savedSettings.LimitTaggingToPcGames;
                AutomaticallyAddTagsToNewGames = savedSettings.AutomaticallyAddTagsToNewGames;
                LastAutomaticTagUpdate = savedSettings.LastAutomaticTagUpdate;
                OkayTags = new ObservableCollection<string>(savedSettings.OkayTags.OrderBy(a => a));
                BlacklistedTags = new ObservableCollection<string>(savedSettings.BlacklistedTags.OrderBy(a => a));
                LanguageKey = savedSettings.LanguageKey;
            }

            if (LastAutomaticTagUpdate == default)
            {
                LastAutomaticTagUpdate = DateTime.Now;
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(this);
            plugin.Settings = this;
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }

        private Dictionary<string, string> _languages = new Dictionary<string, string>
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
            {"hungarian","Magyar (Hungarian)"},
            {"dutch","Nederlands (Dutch)"},
            {"norwegian","Norsk (Norwegian)"},
            {"polish","Polski (Polish)"},
            {"portuguese","Português (Portuguese)"},
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
            get => new RelayCommand<IList<object>>((selectedItems) =>
            {
                var selectedStrings = selectedItems.Cast<string>().ToList();
                foreach (string str in selectedStrings)
                {
                    BlacklistedTags.Remove(str);
                    OkayTags.Add(str);
                }
            }, (a) => a?.Count > 0);
        }

        [DontSerialize]
        public RelayCommand<IList<object>> BlacklistCommand
        {
            get => new RelayCommand<IList<object>>((selectedItems) =>
            {
                var selectedStrings = selectedItems.Cast<string>().ToList();
                foreach (string str in selectedStrings)
                {
                    OkayTags.Remove(str);
                    BlacklistedTags.Add(str);
                }
            }, (a) => a?.Count > 0);
        }
    }
}