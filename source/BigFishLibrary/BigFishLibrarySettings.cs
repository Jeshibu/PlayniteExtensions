using BigFishMetadata;
using Playnite.SDK;
using System;
using System.Windows.Media;

namespace BigFishLibrary;

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
                if (BigFishOnlineLibraryScraper.IsLoggedIn(view))
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

    public RelayCommand<object> LoginCommand => new(a => Login());

    private void Login()
    {
        try
        {
            PlayniteApi.Dialogs.ShowMessage(
                "Close the login browser window once you're logged in. Due to website changes and limitations this plugin cannot automatically close the window once you're logged in anymore.",
                "Close the next window yourself!",
                System.Windows.MessageBoxButton.OK);

            using (var view = PlayniteApi.WebViews.CreateView(675, 540, Colors.Black))
            {
                view.DeleteDomainCookies(".bigfishgames.com");
                view.DeleteDomainCookies("bigfishgames.com");
                view.DeleteDomainCookies(".www.bigfishgames.com");
                view.DeleteDomainCookies("www.bigfishgames.com");
                view.Navigate(BigFishOnlineLibraryScraper.OrderHistoryUrl);

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