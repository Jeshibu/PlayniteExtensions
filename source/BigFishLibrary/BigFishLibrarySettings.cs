using BigFishMetadata;
using Playnite.SDK;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BigFishLibrary
{
    public class BigFishLibrarySettings : BigFishMetadataSettings
    {
        private bool importFromOnline = false;

        public bool ImportFromOnline { get => importFromOnline; set => SetValue(ref importFromOnline, value); }
    }

    public enum AuthStatus
    {
        Ok,
        Checking,
        AuthRequired,
        Failed,
    }

    public class BigFishLibrarySettingsViewModel : PluginSettingsViewModel<BigFishLibrarySettings, BigFishLibrary>
    {
        public BigFishLibrarySettingsViewModel(BigFishLibrary plugin) : base(plugin, plugin.PlayniteApi)
        {
            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<BigFishLibrarySettings>();

            // LoadPluginSettings returns null if no saved data is available.
            Settings = savedSettings ?? new BigFishLibrarySettings();
        }

        public AuthStatus AuthStatus
        {
            get
            {
                var view = PlayniteApi.WebViews.CreateOffscreenView();
                try
                {
                    view.NavigateAndWait(accountUrl);
                    string actualUrl = view.GetCurrentAddress(); //this will be a login URL if not authenticated

                    if (actualUrl == accountUrl)
                        return AuthStatus.Ok;
                    else
                        return AuthStatus.AuthRequired;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to check Big Fish Games auth status.");
                    return AuthStatus.Failed;
                }
                finally
                {
                    view.Close();
                    view.Dispose();
                }
            }
        }

        public RelayCommand<object> LoginCommand => new RelayCommand<object>(a => Login());

        private const string accountUrl = "https://susi.bigfishgames.com/edit";

        private void Login()
        {
            try
            {
                using (var view = PlayniteApi.WebViews.CreateView(675, 540, Colors.Black))
                {
                    view.DeleteDomainCookies(".bigfishgames.com");
                    view.DeleteDomainCookies("bigfishgames.com");
                    view.DeleteDomainCookies(".www.bigfishgames.com");
                    view.DeleteDomainCookies("www.bigfishgames.com");
                    view.Navigate(accountUrl);

                    view.LoadingChanged += (s, e) =>
                    {
                        try
                        {
                            if (e.IsLoading)
                                return;

                            var address = view.GetCurrentAddress();
                            if (address == accountUrl)
                            {
                                view.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Error logging into Big Fish Games");
                        }
                    };

                    view.OpenDialog();
                }

                OnPropertyChanged(nameof(AuthStatus));
            }
            catch (Exception e)
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Error logging in to Big Fish Games", "");
                Logger.Error(e, "Failed to authenticate user.");
            }
        }
    }
}