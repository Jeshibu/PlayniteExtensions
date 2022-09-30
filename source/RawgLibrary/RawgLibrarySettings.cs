using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rawg.Common;
using System.Diagnostics;
using Newtonsoft.Json;

namespace RawgLibrary
{
    public class RawgLibrarySettings : RawgBaseSettings
    {
        private string username;
        private bool importUserLibrary = true;
        private List<RawgCollectionSetting> collections = new List<RawgCollectionSetting>();

        public string Username { get => username; set => SetValue(ref username, value); }
        public bool ImportUserLibrary { get => importUserLibrary; set => SetValue(ref importUserLibrary, value); }
        public List<RawgCollectionSetting> Collections { get => collections; set => SetValue(ref collections, value); }
    }

    public class RawgCollectionSetting : ObservableObject
    {
        public RawgCollectionSetting() { }

        public RawgCollectionSetting(RawgCollection collection)
        {
            Collection = collection;
        }

        public RawgCollection Collection { get; set; }
        public bool Import { get; set; }

        [JsonIgnore]
        public int Id { get => Collection.Id; }

        [JsonIgnore]
        public string Name { get => $"{Collection.Name} ({Collection.GamesCount})"; }
    }

    public class RawgLibrarySettingsViewModel : PluginSettingsViewModel<RawgLibrarySettings, RawgLibrary>
    {
        public RawgLibrarySettingsViewModel(RawgLibrary plugin) : base(plugin, plugin.PlayniteApi)
        {
            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<RawgLibrarySettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new RawgLibrarySettings();
            }
        }

        public RelayCommand<object> LoginCommand
        {
            get => new RelayCommand<object>(a =>
            {
                Process.Start(@"https://rawg.io/login?forward=developer");
            });
        }

        public RelayCommand<object> LanguageCodesReferenceCommand
        {
            get => new RelayCommand<object>(a =>
            {
                Process.Start(@"https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes");
            });
        }

        public RelayCommand<object> RefreshCollectionsCommand
        {
            get => new RelayCommand<object>(a =>
            {
                try
                {
                    var client = GetApiClient();
                    if (client == null || string.IsNullOrWhiteSpace(Settings.Username))
                        return;

                    var apiCollections = client.GetCollections(Settings.Username);
                    Settings.Collections = apiCollections.Results.Select(c => new RawgCollectionSetting(c)).ToList();
                }
                catch(Exception ex)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage("Error fetching user collections: " + ex.Message, "Error");
                }
            });
        }

        private RawgApiClient GetApiClient()
        {
            if (string.IsNullOrWhiteSpace(Settings.ApiKey))
                return null;

            return new RawgApiClient(Plugin.Downloader, Settings.ApiKey);
        }
    }
}