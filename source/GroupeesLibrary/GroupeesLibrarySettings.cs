using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace GroupeesLibrary
{
    public class GroupeesLibrarySettings : ObservableObject
    {
        private int userId;
        private List<Cookie> cookies = new List<Cookie>();
        private Dictionary<string, GameInstallInfo> installData = new Dictionary<string, GameInstallInfo>();
        private string installationDirectory;
        private string downloadDirectory;

        public int UserId { get => userId; set => SetValue(ref userId, value); }

        public List<Cookie> Cookies { get => cookies; set => SetValue(ref cookies, value); }

        public Dictionary<string,GameInstallInfo> InstallData { get => installData; set => SetValue(ref installData, value); }

        public string InstallationDirectory { get => installationDirectory; set => SetValue(ref installationDirectory, value); }

        public string DownloadDirectory { get => downloadDirectory; set => SetValue(ref downloadDirectory, value); }
    }

    public enum AuthStatus
    {
        Ok,
        Checking,
        AuthRequired,
        Failed
    }

    public class GameInstallInfo
    {
        public string Name { get; set; }
        public string DownloadUrl { get; set; }
        public string InstallLocation { get; set; }
        public string RelativeExecutablePath { get; set; }
    }

    public class GroupeesLibrarySettingsViewModel : PluginSettingsViewModel<GroupeesLibrarySettings, GroupeesLibrary>
    {
        public AuthStatus AuthStatus
        {
            get
            {
                if (Settings.UserId == 0 || !Settings.Cookies.Any())
                {
                    return AuthStatus.AuthRequired;
                }

                try
                {
                    if (Plugin.IsAuthenticated(Settings))
                        return AuthStatus.Ok;
                    else
                        return AuthStatus.AuthRequired;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to check Groupees auth status.");
                    return AuthStatus.Failed;
                }
                finally
                {
                    Settings.Cookies = Plugin.Downloader.Cookies.Cast<Cookie>().ToList();
                }
            }
        }

        public GroupeesLibrarySettingsViewModel(GroupeesLibrary plugin, IPlayniteAPI playniteAPI) : base(plugin, playniteAPI)
        {
            var savedSettings = plugin.LoadPluginSettings<GroupeesLibrarySettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new GroupeesLibrarySettings();
            }
        }

        public RelayCommand<object> LoginCommand
        {
            get => new RelayCommand<object>((a) =>
            {
                Login();
            });
        }

        private void Login()
        {
            try
            {
                string loginUrl = "https://groupees.com/login";
                int userId = 0;
                List<Cookie> cookies = new List<Cookie>();
                using (var view = PlayniteApi.WebViews.CreateView(675, 540, Colors.Black))
                {
                    view.LoadingChanged += async (s, e) =>
                    {
                        var address = view.GetCurrentAddress();
                        if (address != loginUrl && !address.StartsWith("https://groupees.com/auth/"))
                        {
                            var source = await view.GetPageSourceAsync();
                            var idMatch = Regex.Match(source, @"/user_walls/(\d+)");
                            if (idMatch.Success)
                            {
                                userId = int.Parse(idMatch.Groups[1].Value);
                                cookies.AddRange(view.GetCookies().Where(c => c.Domain.EndsWith("groupees.com")).Select(PlayniteConvert.ToCookie));
                                view.Close();
                            }
                        }
                    };

                    view.DeleteDomainCookies(".groupees.com");
                    view.DeleteDomainCookies("groupees.com");
                    view.Navigate(loginUrl);
                    view.OpenDialog();
                }

                if (userId != 0)
                {
                    Settings.UserId = userId;
                    Settings.Cookies = cookies;
                }

                OnPropertyChanged(nameof(AuthStatus));
            }
            catch (Exception e)
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Error logging in to Groupees", "");
                Logger.Error(e, "Failed to authenticate user.");
            }
        }
    }
}