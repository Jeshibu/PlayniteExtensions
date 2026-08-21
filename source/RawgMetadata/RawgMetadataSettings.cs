using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;
using Rawg.Common;
using RawgMetadata.Database;
using System;
using System.Diagnostics;

namespace RawgMetadata;

public class RawgMetadataSettings : RawgBaseSettings, IBulkImportPluginSettings
{
    public bool ShowTopPanelButton { get; set; }
    public int MaxDegreeOfParallelism { get; set; }
    public int Version { get; set; }
}

public class RawgMetadataSettingsViewModel : PluginSettingsViewModel<RawgMetadataSettings, RawgMetadata>
{
    public RawgMetadataSettingsViewModel(RawgMetadata plugin) : base(plugin, plugin.PlayniteApi)
    {
        Settings = LoadSavedSettings();
        if (Settings == null)
        {
            Settings = new();
            BulkImportPluginSettings.Initialize(Settings);
        }
    }

    public RelayCommand<object> LoginCommand
    {
        get => new(_ =>
        {
            Process.Start("https://rawg.io/login?forward=developer");
        });
    }

    public RelayCommand<object> LanguageCodesReferenceCommand
    {
        get => new(_ =>
        {
            Process.Start("https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes");
        });
    }

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

    public RelayCommand<object> DownloadTagsCommand
    {
        get => new(_ =>
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
}
