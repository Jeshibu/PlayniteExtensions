using EaLibrary.Services;
using Playnite.SDK;

namespace EaLibrary;

public class EaLibrarySettings
{
    public int Version { get; set; }
    public bool ConnectAccount { get; set; } = true;
    public bool ImportInstalledGames { get; set; } = true;
    public bool ImportUninstalledGames { get; set; } = true;
}

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
