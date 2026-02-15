using GOGMetadata.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GOGMetadata;

public class GOGMetadataSettings : ObservableObject
{
    public bool UseVerticalCovers { get; set; } = true;
    public string Locale { get; set=> SetValue(ref field, value); } = "en";
    public ObservableCollection<BackgroundType> BackgroundTypePriority { get; set; }
    public List<string> BlacklistedTags { get; set; } = [];
}

public class GOGMetadataSettingsViewModel : PluginSettingsViewModel<GOGMetadataSettings, GOGMetadata>
{
    private readonly GogApi _gogApi;

    public GOGMetadataSettingsViewModel(GOGMetadata plugin, IPlayniteAPI playniteApi, GogApi gogApi) : base(plugin, playniteApi)
    {
        _gogApi = gogApi;
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
        if (Languages.Any(l => l.Slug == langCode))
            Settings.Locale = langCode;
    }

    public RelayCommand InitializeCatalogDataCommand => new(InitializeCatalogData);

    public void InitializeCatalogData()
    {
        try
        {
            var catalogData = _gogApi.GetCatalogData(Settings.Locale);
            if (catalogData.Languages.Any())
            {
                var locale = Settings.Locale;
                Languages.Clear();
                Languages.AddMissing(catalogData.Languages);
                Settings.Locale = locale;
            }

            if (catalogData.FullTagsList.Any())
            {
                OkayTags.Clear();
                BlacklistedTags.Clear();
                foreach (var tag in catalogData.FullTagsList)
                {
                    if (Settings.BlacklistedTags.Contains(tag.Slug))
                        BlacklistedTags.Add(tag);
                    else
                        OkayTags.Add(tag);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to load catalog data");
            PlayniteApi.Dialogs.ShowErrorMessage("Failed to load online tag data");
        }
    }

    public ObservableCollection<SluggedName> Languages { get; } =
    [
        new("en", "English"),
        new("de", "Deutsch"),
        new("fr", "Français"),
        new("pl", "Polski"),
        new("ru", "Pусский"),
        new("zh", "中文(简体)")
    ];

    public ObservableCollection<SluggedName> OkayTags
    {
        get;
        set => SetValue(ref field, value);
    }

    public ObservableCollection<SluggedName> BlacklistedTags
    {
        get;
        set => SetValue(ref field, value);
    }

    public RelayCommand<IList<object>> WhitelistCommand => new(selectedItems =>
    {
        var selectedKeyValuePairs = selectedItems.Cast<SluggedName>().ToList();
        foreach (var sel in selectedKeyValuePairs)
        {
            BlacklistedTags.Remove(sel);
            OkayTags.Add(sel);
            Settings.BlacklistedTags.Remove(sel.Slug);
        }
    }, canExecute: a => a?.Count > 0);

    public RelayCommand<IList<object>> BlacklistCommand => new(selectedItems =>
    {
        var selectedKeyValuePairs = selectedItems.Cast<SluggedName>().ToList();
        foreach (var sel in selectedKeyValuePairs)
        {
            OkayTags.Remove(sel);
            BlacklistedTags.Add(sel);
            Settings.BlacklistedTags.Add(sel.Slug);
        }
    }, canExecute: a => a?.Count > 0);
}

public enum BackgroundType
{
    Screenshot,
    Background,
    StoreBackground
}
