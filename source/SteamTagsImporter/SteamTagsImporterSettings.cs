using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SteamTagsImporter
{
    public class SteamTagsImporterSettings : ISettings
    {
        private readonly SteamTagsImporter plugin;

        public bool LimitTagsToFixedAmount { get; set; } = false;

        public int FixedTagCount { get; set; } = 5;

        public bool AutomaticallyAddTagsToNewGames { get; set; } = true;

        public ObservableCollection<string> OkayTags { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<string> BlacklistedTags { get; set; } = new ObservableCollection<string>();

        public DateTime LastAutomaticTagUpdate { get; set; } = default;

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

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                LimitTagsToFixedAmount = savedSettings.LimitTagsToFixedAmount;
                FixedTagCount = savedSettings.FixedTagCount;
                AutomaticallyAddTagsToNewGames = savedSettings.AutomaticallyAddTagsToNewGames;
                LastAutomaticTagUpdate = savedSettings.LastAutomaticTagUpdate;
                OkayTags = new ObservableCollection<string>(savedSettings.OkayTags.OrderBy(a=>a));
                BlacklistedTags = new ObservableCollection<string>(savedSettings.BlacklistedTags.OrderBy(a=>a));
            }

            if(LastAutomaticTagUpdate == default)
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