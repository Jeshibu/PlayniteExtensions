using BigFishMetadata;
using Playnite.SDK;
using System;

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
            var scraper = new BigFishOnlineLibraryScraper(Plugin.PlayniteApi);
            try
            {
                if (scraper.IsAuthenticated())
                    return AuthStatus.Ok;
                else
                    return AuthStatus.AuthRequired;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to check Big Fish Games auth status.");
                return AuthStatus.Failed;
            }
        }
    }

    public RelayCommand<object> LoginCommand => new(_ => Login());

    private void Login()
    {
        bool success = new BigFishOnlineLibraryScraper(Plugin.PlayniteApi).Authenticate();
        OnPropertyChanged(nameof(AuthStatus));
    }
}
