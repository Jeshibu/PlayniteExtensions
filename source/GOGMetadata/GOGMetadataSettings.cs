using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GOGMetadata;

public class GOGMetadataSettings : ObservableObject
{
    public bool UseVerticalCovers { get; set; } = true;
    public string Locale { get; set; } = "en";
    public ObservableCollection<BackgroundType> BackgroundTypePriority { get; set; }
}

public class GOGMetadataSettingsViewModel : PluginSettingsViewModel<GOGMetadataSettings, GOGMetadata>
{
    public GOGMetadataSettingsViewModel(GOGMetadata plugin, IPlayniteAPI playniteApi) : base(plugin, playniteApi)
    {
        Settings = LoadSavedSettings();

        if (Settings == null)
        {
            Settings = new();
            SetMetadataLanguageByPlayniteLanguage();
        }

        Settings.BackgroundTypePriority ??= Enum.GetValues(typeof(BackgroundType)).OfType<BackgroundType>().ToObservable();
    }

    private void SetMetadataLanguageByPlayniteLanguage()
    {
        var langCode = PlayniteApi.ApplicationSettings.Language.Substring(0, 2);
        if (Languages.ContainsKey(langCode))
            Settings.Locale = langCode;
    }

    public Dictionary<string, string> Languages { get; } = new()
    {
        { "en", "English" },
        { "de", "Deutsch" },
        { "fr", "Français" },
        { "pl", "Polski" },
        { "ru", "Pусский" },
        { "zh", "中文(简体)" },
    };
}

public enum BackgroundType
{
    Screenshot,
    Background,
    StoreBackground
}
