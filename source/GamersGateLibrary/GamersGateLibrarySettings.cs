using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GamersGateLibrary
{
    public class GamersGateLibrarySettings : ObservableObject
    {
        private List<Cookie> cookies = new List<Cookie>();
        private Dictionary<string, GameInstallInfo> installData = new Dictionary<string, GameInstallInfo>();
        private bool useCoverImages = true;

        public List<Cookie> Cookies { get => cookies; set => SetValue(ref cookies, value); }

        public Dictionary<string, GameInstallInfo> InstallData { get => installData; set => SetValue(ref installData, value); }

        public bool UseCoverImages { get => useCoverImages; set => SetValue(ref useCoverImages, value); }
    }

    public class GameInstallInfo
    {
        public string Id { get; set; }
        public int OrderId { get; set; }
        public string Name { get; set; }
        public List<DownloadUrl> DownloadUrls { get; set; } = new List<DownloadUrl>();
        public string InstallLocation { get; set; }
        public string RelativeExecutablePath { get; set; }
    }

    public class DownloadUrl
    {
        public string Description { get; set; }
        public string Url { get; set; }
    }


    public enum AuthStatus
    {
        Ok,
        Checking,
        AuthRequired,
        Failed
    }

    public class GamersGateLibrarySettingsViewModel : PluginSettingsViewModel<GamersGateLibrarySettings, GamersGateLibrary>
    {
        public AuthStatus AuthStatus
        {
            get
            {
                if (!Settings.Cookies.Any())
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
                    Logger.Error(e, "Failed to check GamersGate auth status.");
                    return AuthStatus.Failed;
                }
                finally
                {
                    Settings.Cookies = Plugin.Downloader.Cookies.Cast<Cookie>().ToList();
                }
            }
        }

        public GamersGateLibrarySettingsViewModel(GamersGateLibrary plugin, IPlayniteAPI playniteAPI) : base(plugin, playniteAPI)
        {
            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<GamersGateLibrarySettings>();

            // LoadPluginSettings returns null if not saved data is available.
            Settings = savedSettings ?? new GamersGateLibrarySettings();
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
                string loginUrl = "https://www.gamersgate.com/login/";
                int userId = 0;
                List<Cookie> cookies = new List<Cookie>();
                using (var view = PlayniteApi.WebViews.CreateView(675, 540, Colors.Black))
                {
                    view.LoadingChanged += async (s, e) =>
                    {
                        try
                        {
                            var address = view.GetCurrentAddress();
                            if (!address.StartsWith(loginUrl))
                            {
                                var source = await view.GetPageSourceAsync();
                                var idMatch = Regex.Match(source, @"/images/avatar/current/(\d+)");
                                if (idMatch.Success)
                                {
                                    userId = int.Parse(idMatch.Groups[1].Value);
                                    cookies.AddRange(view.GetCookies().Where(c => c.Domain.EndsWith("gamersgate.com")).Select(PlayniteConvert.ToCookie).ToList());
                                    view.Close();
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            Logger.Error(ex, "Error logging into GamersGate");
                        }
                    };

                    view.DeleteDomainCookies(".gamersgate.com");
                    view.DeleteDomainCookies("gamersgate.com");
                    view.DeleteDomainCookies(".www.gamersgate.com");
                    view.DeleteDomainCookies("www.gamersgate.com"); //this is the only one currently hit, the rest is future proofing
                    view.Navigate(loginUrl);
                    view.OpenDialog();
                }

                if (userId != 0)
                {
                    //Settings.UserId = userId; //userId isn't really used anywhere else
                    Settings.Cookies = cookies;
                }

                OnPropertyChanged(nameof(AuthStatus));
            }
            catch (Exception e)
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Error logging in to GamersGate", "");
                Logger.Error(e, "Failed to authenticate user.");
            }
        }
    }
}