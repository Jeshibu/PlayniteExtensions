using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace itchioBundleTagger
{
    public class itchioBundleTaggerSettings : ISettings
    {
        private readonly itchioBundleTagger plugin;

        [DontSerialize]
        public Labels Labels { get; }

        public bool UseTagPrefix { get; set; } = true;

        public string TagPrefix { get; set; } = "[itch.io] ";

        public bool AddAvailableOnSteamTag { get; set; } = true;

        public bool AddFreeTag { get; set; } = false;

        public bool AddSteamLink { get; set; } = true;

        public bool RunOnLibraryUpdate { get; set; } = true;

        public bool ShowInContextMenu { get; set; } = false;

        public Dictionary<string, Guid> TagIds { get; set; } = new Dictionary<string, Guid>();

        // Parameterless constructor must exist if you want to use LoadPluginSettings method.
        public itchioBundleTaggerSettings()
        {
        }

        public itchioBundleTaggerSettings(itchioBundleTagger plugin, itchIoTranslator translator)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;
            Labels = new Labels(translator);

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<itchioBundleTaggerSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                UseTagPrefix = savedSettings.UseTagPrefix;
                TagPrefix = savedSettings.TagPrefix;
                AddAvailableOnSteamTag = savedSettings.AddAvailableOnSteamTag;
                AddFreeTag = savedSettings.AddFreeTag;
                AddSteamLink = savedSettings.AddSteamLink;
                TagIds = savedSettings.TagIds;
                RunOnLibraryUpdate = savedSettings.RunOnLibraryUpdate;
                ShowInContextMenu = savedSettings.ShowInContextMenu;
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
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }

    public class Labels
    {
        public Labels(itchIoTranslator translator)
        {
            Translator = translator;
        }

        private itchIoTranslator Translator { get; }

        public string UseTagPrefix => Translator.TagPrefixSetting;
        public string AddFreeTag => Translator.AddFreeTagSetting;
        public string AddAvailableOnSteamTag => Translator.AddSteamTagSetting;
        public string AddSteamLink => Translator.AddSteamLinkSetting;
        public string RunOnLibraryUpdate => Translator.RunOnLibraryUpdate;
        public string ShowInContextMenu => Translator.ShowInContextMenu;
    }
}