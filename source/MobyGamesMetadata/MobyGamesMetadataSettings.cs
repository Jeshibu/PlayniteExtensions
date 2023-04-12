using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MobyGamesMetadata
{
    public class MobyGamesMetadataSettings : ObservableObject
    {
        private string apiKey;

        public DataSource DataSource { get; set; } = DataSource.Api;
        public string ApiKey
        {
            get { return apiKey; }
            set
            {
                apiKey = value?.Trim();
                OnPropertyChanged(nameof(ApiKey));
            }
        }
        public bool ShowTopPanelButton { get; set; } = true;
    }

    [Flags]
    public enum DataSource
    {
        None = 0,
        Api = 1,
        Scraping = 2,
        ApiAndScraping = 3,
    }

    public class MobyGamesMetadataSettingsViewModel : PluginSettingsViewModel<MobyGamesMetadataSettings, MobyGamesMetadata>
    {
        public MobyGamesMetadataSettingsViewModel(MobyGamesMetadata plugin) : base(plugin, plugin.PlayniteApi)
        {
            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<MobyGamesMetadataSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new MobyGamesMetadataSettings();
            }
        }

        public RelayCommand<object> GetApiKeyCommand
        {
            get => new RelayCommand<object>((a) =>
            {
                Process.Start(@"https://www.mobygames.com/info/api/");
            });
        }

        public PropertyImportTarget[] ImportTargets { get; } = new[]
        {
            PropertyImportTarget.Ignore,
            PropertyImportTarget.Genres,
            PropertyImportTarget.Tags,
            PropertyImportTarget.Series,
            PropertyImportTarget.Features,
        };
    }
}