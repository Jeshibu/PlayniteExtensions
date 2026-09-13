using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;
using Rawg.Common;
using RawgMetadata.Database;
using System;
using System.Diagnostics;

namespace RawgMetadata;

public class RawgMetadataSettings : RawgBaseSettings, IBulkImportPluginSettings
{
    public bool ShowTopPanelButton { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = BulkImportPluginSettings.GetDefaultMaxDegreeOfParallelism();
    public int Version { get; set; } = 0;
}

public class RawgMetadataSettingsViewModel : PluginSettingsViewModel<RawgMetadataSettings, RawgMetadata>
{
    public RawgMetadataSettingsViewModel(RawgMetadata plugin) : base(plugin, plugin.PlayniteApi)
    {
        Settings = LoadSavedSettings() ?? new();
    }

    public RelayCommand<object> LoginCommand => new(_ =>
    {
        Process.Start("https://rawg.io/login?forward=developer");
    });

    public RelayCommand<object> LanguageCodesReferenceCommand => new(_ =>
    {
        Process.Start("https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes");
    });

    private RawgDatabase Database => new(Plugin.GetPluginUserDataPath());

    public string DatabaseStatus
    {
        get
        {
            var db = Database;
            if (!db.FileExists)
                return "Local database does not exist yet.";

            int tagCount = db.GetTagCount();
            return $"Local database has {tagCount} tags and was last updated {db.FileLastModified:g}";
        }
    }

    public RelayCommand<object> DownloadTagsCommand => new(_ =>
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Settings.ApiKey))
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Please enter your API key first");
                return;
            }

            var apiClient = new RawgApiClient(Settings.ApiKey);
            PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
            {
                try
                {
                    Database.CreateDatabase(apiClient, a);
                }
                catch (Exception ex)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage($"Error saving local database: {ex.Message}");
                }
            }, new("Preparing…", cancelable: true));
            OnPropertyChanged(nameof(DatabaseStatus));
        }
        catch (Exception ex)
        {
            PlayniteApi.Dialogs.ShowErrorMessage(ex.ToString());
        }
    });
}
