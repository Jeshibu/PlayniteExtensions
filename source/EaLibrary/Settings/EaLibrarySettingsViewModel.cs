using Playnite.SDK;

namespace EaLibrary.Settings;

public class EaLibrarySettingsViewModel : PluginSettingsViewModel<EaLibrarySettings, EaLibrary>
{
    public EaLibrarySettingsViewModel(EaLibrary library, IPlayniteAPI api) : base(library, api)
    {
        var savedSettings = LoadSavedSettings();
        if (savedSettings != null)
        {
            if (savedSettings.Version == 0)
            {
                Logger.Debug("Updating EA settings from version 0.");
                if (savedSettings.ImportUninstalledGames)
                {
                    savedSettings.ConnectAccount = true;
                }
            }

            savedSettings.Version = 1;
            Settings = savedSettings;
        }
        else
        {
            Settings = new EaLibrarySettings { Version = 1 };
        }
    }

    public bool IsUserLoggedIn => Plugin.Website.IsAuthenticated();

    public RelayCommand<object> LoginCommand => new(_ =>
    {
        Plugin.Website.Login();
        OnPropertyChanged(nameof(IsUserLoggedIn));
    });
}
