using BigFishMetadata;
using Playnite.SDK;
using Playnite.SDK.Events;
using System;
using System.Windows.Media;

namespace BigFishLibrary;

public class BigFishLibrarySettings : BigFishMetadataSettings
{
    public bool ImportFromOnline
    {
        get;
        set => SetValue(ref field, value);
    } = false;
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
        Settings = LoadSavedSettings() ?? new BigFishLibrarySettings();
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

    public RelayCommand<object> LoginCommand => new(_ => Login());

    private void Login()
    {
        var view = PlayniteApi.WebViews.CreateView(675, 540, Colors.Black);
        view.LoadingChanged += CloseWhenLoggedIn;
        try
        {
            view.DeleteDomainCookies(".bigfishgames.com");
            view.DeleteDomainCookies("bigfishgames.com");
            view.DeleteDomainCookies(".www.bigfishgames.com");
            view.DeleteDomainCookies("www.bigfishgames.com");
            view.Navigate(BigFishOnlineLibraryScraper.OrderHistoryUrl);

            view.OpenDialog();

            OnPropertyChanged(nameof(AuthStatus));
        }
        catch (Exception e)
        {
            PlayniteApi.Dialogs.ShowErrorMessage("Error logging in to Big Fish Games", "");
            Logger.Error(e, "Failed to authenticate user.");
        }
        finally
        {
            view.LoadingChanged -= CloseWhenLoggedIn;
            view.Dispose();
        }
    }

    private static void CloseWhenLoggedIn(object sender, WebViewLoadingChangedEventArgs e)
    {
        var view = (IWebView)sender;
        if (!e.IsLoading && view.GetCurrentAddress() == BigFishOnlineLibraryScraper.OrderHistoryUrl)
            view.Close();
    }
}
