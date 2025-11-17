using Playnite.SDK;
using System.Collections.Generic;

namespace ViveportLibrary;

public class ViveportLibrarySettings : ObservableObject
{
    public bool ImportHeadsetsAsPlatforms{ get; set => SetValue(ref field, value); } = true;
    public bool TagSubscriptionGames{ get; set => SetValue(ref field, value); } = false;
    public string SubscriptionTagName{ get; set => SetValue(ref field, value); } = "Subscription";
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
        get => new(a =>
        {
            Plugin.SetSubscriptionTags();
        });
    }

    public Dictionary<CoverPreference, string> CoverPreferenceOptions
    => new()
    {
        { CoverPreference.None, "None" },
        { CoverPreference.VerticalOrSquare, "Vertical (fallback to square)" },
        { CoverPreference.VerticalOrBust, "Vertical (no fallback)" },
        { CoverPreference.Square, "Square" },
        { CoverPreference.Horizontal, "Horizontal" },
    };
}
