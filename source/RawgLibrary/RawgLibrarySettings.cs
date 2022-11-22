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
using Playnite.SDK.Models;
using System.Collections.ObjectModel;

namespace RawgLibrary
{
    public class RawgLibrarySettings : RawgBaseSettings
    {
        private string userToken;
        private bool importUserLibrary = true;
        private List<RawgCollectionSetting> collections = new List<RawgCollectionSetting>();
        private Dictionary<string, Guid?> rawgToPlayniteStatuses;
        private Dictionary<int, int> rawgToPlayniteRatings;
        private Dictionary<Guid, string> playniteToRawgStatuses;
        private Dictionary<int, Range> playniteToRawgRatings;
        private RawgUser user;


        public string UserToken { get => userToken; set => SetValue(ref userToken, value); }
        public bool ImportUserLibrary { get => importUserLibrary; set => SetValue(ref importUserLibrary, value); }
        public List<RawgCollectionSetting> Collections { get => collections; set => SetValue(ref collections, value); }
        public Dictionary<string, Guid?> RawgToPlayniteStatuses { get => rawgToPlayniteStatuses; set => SetValue(ref rawgToPlayniteStatuses, value); }
        public Dictionary<int, int> RawgToPlayniteRatings { get => rawgToPlayniteRatings; set => SetValue(ref rawgToPlayniteRatings, value); }
        public Dictionary<Guid, string> PlayniteToRawgStatuses { get => playniteToRawgStatuses; set => SetValue(ref playniteToRawgStatuses, value); }
        public Dictionary<int, Range> PlayniteToRawgRatings { get => playniteToRawgRatings; set => SetValue(ref playniteToRawgRatings, value); }
        public RawgUser User { get => user; set => SetValue(ref user, value); }
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

        public override void BeginEdit()
        {
            base.BeginEdit();
            InitializeCollections();
        }

        public override void EndEdit()
        {
            SetSettingsCollections();
            base.EndEdit();
        }

        public RelayCommand<object> GetApiKeyCommand
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

        public RelayCommand<object> LoginCommand
        {
            get => new RelayCommand<object>(a =>
            {
                var window = Plugin.PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions { ShowCloseButton = true, ShowMaximizeButton = true, ShowMinimizeButton = false });
                var loginPrompt = new LoginPrompt(window);
                window.Content = loginPrompt;
                window.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
                var dialogResult = window.ShowDialog();
                if (dialogResult == true)
                {
                    Settings.User = null;
                    Settings.UserToken = null;
                    string email = loginPrompt.EmailAddress;
                    string password = loginPrompt.Password;
                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    {
                        Plugin.PlayniteApi.Dialogs.ShowErrorMessage("Email or password empty. Login failed.", "Login failed");
                        return;
                    }

                    string token;
                    try
                    {
                        var client = GetApiClient();
                        token = client.Login(email, password);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Login failed");
                        Plugin.PlayniteApi.Dialogs.ShowErrorMessage($"Login failed: {ex.Message}", "Login failed");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(token))
                    {
                        Plugin.PlayniteApi.Dialogs.ShowErrorMessage("Empty token, login failed.", "Login failed");
                        return;
                    }
                    Settings.UserToken = token;
                    RefreshCollections();
                    OnPropertyChanged(nameof(AuthenticationStatus));
                }
            });
        }

        private void RefreshCollections()
        {
            try
            {
                var client = GetApiClient();
                var apiCollections = client.GetCurrentUserCollections(Settings.UserToken);
                Settings.Collections = apiCollections.Select(c => new RawgCollectionSetting(c)).ToList();
            }
            catch (Exception ex)
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Error fetching user collections: " + ex.Message, "Error");
            }
        }

        public RelayCommand<object> RefreshCollectionsCommand
        {
            get => new RelayCommand<object>(a =>
            {
                RefreshCollections();
            });
        }

        public string AuthenticationStatus
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(Settings.UserToken))
                        return "❌ Not authenticated";

                    var client = GetApiClient();
                    var user = client.GetCurrentUser(Settings.UserToken);
                    Settings.User = user;
                    string output = $"✔ Authenticated as {user.Username}\n";
                    if (user.ApiKey != null)
                        output += "✔ API key present";
                    else
                        output += "❌ API key not present";
                    return output;
                }
                catch (Exception ex)
                {
                    return $"❌ Error: {ex.Message}";
                }
            }
        }

        private RawgApiClient GetApiClient()
        {
            return new RawgApiClient(Settings.ApiKey);
        }

        private ObservableCollection<RawgToPlayniteStatus> rawgToPlayniteStatuses;
        private ObservableCollection<RawgToPlayniteRating> rawgToPlayniteRatings;
        private ObservableCollection<PlayniteToRawgStatus> playniteToRawgStatuses;
        private ObservableCollection<PlayniteToRawgRating> playniteToRawgRatings;
        private ICollection<CompletionStatus> playniteCompletionStatuses;
        private Dictionary<string, string> rawgCompletionStatuses;

        public ObservableCollection<RawgToPlayniteStatus> RawgToPlayniteStatuses { get => rawgToPlayniteStatuses; set => SetValue(ref rawgToPlayniteStatuses, value); }
        public ObservableCollection<RawgToPlayniteRating> RawgToPlayniteRatings { get => rawgToPlayniteRatings; set => SetValue(ref rawgToPlayniteRatings, value); }
        public ObservableCollection<PlayniteToRawgStatus> PlayniteToRawgStatuses { get => playniteToRawgStatuses; set => SetValue(ref playniteToRawgStatuses, value); }
        public ObservableCollection<PlayniteToRawgRating> PlayniteToRawgRatings { get => playniteToRawgRatings; set => SetValue(ref playniteToRawgRatings, value); }
        public ICollection<CompletionStatus> PlayniteCompletionStatuses { get => playniteCompletionStatuses; set => SetValue(ref playniteCompletionStatuses, value); }
        public Dictionary<string, string> RawgCompletionStatuses { get => rawgCompletionStatuses; set => SetValue(ref rawgCompletionStatuses, value); }

        private void InitializeCollections()
        {
            PlayniteCompletionStatuses = PlayniteApi.Database.CompletionStatuses;
            RawgCompletionStatuses = RawgMapping.RawgCompletionStatuses;
            RawgToPlayniteStatuses = RawgMapping.GetRawgToPlayniteCompletionStatuses(PlayniteApi, Settings).ToObservable();
            RawgToPlayniteRatings = RawgMapping.GetRawgToPlayniteRatings(Settings).ToObservable();
            PlayniteToRawgStatuses = RawgMapping.GetPlayniteToRawgStatuses(PlayniteApi, Settings).ToObservable();
            PlayniteToRawgRatings = RawgMapping.GetPlayniteToRawgRatings(Settings).ToObservable();
        }

        private void SetSettingsCollections()
        {
            Settings.RawgToPlayniteStatuses = RawgToPlayniteStatuses.ToDictionary(x => x.Id, x => x?.PlayniteCompletionStatusId);
            Settings.RawgToPlayniteRatings = RawgToPlayniteRatings.ToDictionary(x => x.Id, x => x.PlayniteRating);
            Settings.PlayniteToRawgStatuses = PlayniteToRawgStatuses.ToDictionary(x => x.PlayniteCompletionStatus.Id, x => x.RawgStatusId);
            Settings.PlayniteToRawgRatings = PlayniteToRawgRatings.ToDictionary(x => x.Id, x => x.Range);
        }

        private class RawgStatusData
        {
            public string Key;
            public string Description;
            public string PlayniteDefaultStatus;

            public RawgStatusData(string key, string description, string playniteDefaultStatus)
            {
                Key = key;
                Description = description;
                PlayniteDefaultStatus = playniteDefaultStatus;
            }
        }

        private class RawgRatingDefault
        {
            public RawgRatingDefault(int id, string description, string minPlayniteRating, string maxPlayniteRating, string playniteRating)
            {
                Id = id;
                Description = description;
                MinPlayniteRating = minPlayniteRating;
                MaxPlayniteRating = maxPlayniteRating;
                PlayniteRating = playniteRating;
            }

            public int Id { get; }
            public string Description { get; }
            public string MinPlayniteRating { get; }
            public string MaxPlayniteRating { get; }
            public string PlayniteRating { get; }
        }

        /*
    owned
    UncategorizedactiveI'll pick the category later
    playing
    Currently playingI play the game regularly
    beaten
    CompletedI reached my goal in the game
    dropped
    PlayedI gave up and won't play it anymore
    yet
    Not playedI'll play it later

        "Not Played", "Played", "Beaten", "Completed", "Playing", "Abandoned", "On Hold", "Plan to Play"
         */
    }


}