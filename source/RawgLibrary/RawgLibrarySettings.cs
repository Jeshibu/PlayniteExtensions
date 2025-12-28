using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using Rawg.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace RawgLibrary;

public class RawgLibrarySettings : RawgBaseSettings
{
    public string UserToken { get; set => SetValue(ref field, value); }
    public bool ImportUserLibrary { get; set => SetValue(ref field, value); } = true;
    public List<RawgCollectionSetting> Collections { get; set => SetValue(ref field, value); } = [];
    public Dictionary<string, Guid?> RawgToPlayniteStatuses { get; set => SetValue(ref field, value); }
    public Dictionary<int, int> RawgToPlayniteRatings { get; set => SetValue(ref field, value); }
    public Dictionary<Guid, string> PlayniteToRawgStatuses { get; set => SetValue(ref field, value); }
    public Dictionary<int, Range> PlayniteToRawgRatings { get; set => SetValue(ref field, value); }
    public RawgUser User { get; set => SetValue(ref field, value); }
    public bool AutoSyncCompletionStatus { get; set; }
    public bool AutoSyncUserScore { get; set; }
    public bool AutoSyncNewGames { get; set; }
    public bool AutoSyncDeletedGames { get; set; }
    public bool AutoSyncHiddenGames { get; set; } = false;
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
        Settings = LoadSavedSettings() ?? new RawgLibrarySettings();
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
        get => new(a =>
        {
            Process.Start("https://rawg.io/login?forward=developer");
        });
    }

    public RelayCommand<object> LanguageCodesReferenceCommand
    {
        get => new(a =>
        {
            Process.Start("https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes");
        });
    }

    public RelayCommand<object> LoginCommand
    {
        get => new(a =>
        {
            Settings.User = null;
            Settings.UserToken = null;
            var window = Plugin.PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions { ShowCloseButton = true, ShowMaximizeButton = true, ShowMinimizeButton = false });
            var loginPrompt = new LoginPrompt(window);
            window.Content = loginPrompt;
            window.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
            var dialogResult = window.ShowDialog();
            if (dialogResult == true)
            {
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
        get => new(a =>
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
                string output = $"✅ Authenticated as {user.Username}\n";
                if (string.IsNullOrEmpty(user.ApiKey))
                    output += "❌ API key not present";
                else
                    output += "✅ API key present";
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
        return new RawgApiClient(Settings.User?.ApiKey);
    }

    public ObservableCollection<RawgToPlayniteStatus> RawgToPlayniteStatuses{ get; set => SetValue(ref field, value); }
    public ObservableCollection<RawgToPlayniteRating> RawgToPlayniteRatings{ get; set => SetValue(ref field, value); }
    public ObservableCollection<PlayniteToRawgStatus> PlayniteToRawgStatuses{ get; set => SetValue(ref field, value); }
    public ObservableCollection<PlayniteToRawgRating> PlayniteToRawgRatings{ get; set => SetValue(ref field, value); }
    public ICollection<CompletionStatus> PlayniteCompletionStatuses { get; set => SetValue(ref field, value); }
    public Dictionary<string, string> RawgCompletionStatuses{ get; set => SetValue(ref field, value); }

    private void InitializeCollections()
    {
        PlayniteCompletionStatuses = PlayniteApi.Database.CompletionStatuses.ToList();
        PlayniteCompletionStatuses.Add(new() { Id = Guid.Empty, Name = "Default (configure in Library > Library Manager)" });
        PlayniteCompletionStatuses.Add(new() { Id = RawgMapping.DoNotImportId, Name = "Do not import" });
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

    private class RawgStatusData(string key, string description, string playniteDefaultStatus)
    {
        public string Key = key;
        public string Description = description;
        public string PlayniteDefaultStatus = playniteDefaultStatus;
    }

    private class RawgRatingDefault(int id, string description, string minPlayniteRating, string maxPlayniteRating, string playniteRating)
    {
        public int Id { get; } = id;
        public string Description { get; } = description;
        public string MinPlayniteRating { get; } = minPlayniteRating;
        public string MaxPlayniteRating { get; } = maxPlayniteRating;
        public string PlayniteRating { get; } = playniteRating;
    }
}
