using Playnite.SDK;
using System.Collections.Generic;

namespace ViveportLibrary
{
    public class ViveportLibrarySettings : ObservableObject
    {
        private bool importHeadsetsAsPlatforms = true;
        private bool tagSubscriptionGames = false;
        private string subscriptionTagName = "Subscription";

        public bool ImportHeadsetsAsPlatforms { get => importHeadsetsAsPlatforms; set => SetValue(ref importHeadsetsAsPlatforms, value); }
        public bool TagSubscriptionGames { get => tagSubscriptionGames; set => SetValue(ref tagSubscriptionGames, value); }
        public string SubscriptionTagName { get => subscriptionTagName; set => SetValue(ref subscriptionTagName, value); }
        public bool ImportInputMethodsAsFeatures { get; set; } = false;
        public CoverPreference CoverPreference { get; set; } = CoverPreference.VerticalOrSquare;
    }

    public enum CoverPreference
    {
        None,
        VerticalOrSquare,
        VerticalOrBust,
        Square,
        Horizontal,
    }

    public class ViveportLibrarySettingsViewModel : PluginSettingsViewModel<ViveportLibrarySettings, ViveportLibrary>
    {
        public ViveportLibrarySettingsViewModel(ViveportLibrary plugin) : base(plugin, plugin.PlayniteApi)
        {
            // Load saved settings.
            Settings = LoadSavedSettings() ?? new ViveportLibrarySettings();
        }

        public RelayCommand<object> SetSubscriptionTagsCommand
        {
            get => new RelayCommand<object>(a =>
            {
                Plugin.SetSubscriptionTags();
            });
        }

        public Dictionary<CoverPreference, string> CoverPreferenceOptions
        => new Dictionary<CoverPreference, string>
        {
            { CoverPreference.None, "None" },
            { CoverPreference.VerticalOrSquare, "Vertical (fallback to square)" },
            { CoverPreference.VerticalOrBust, "Vertical (no fallback)" },
            { CoverPreference.Square, "Square" },
            { CoverPreference.Horizontal, "Horizontal" },
        };
    }
}