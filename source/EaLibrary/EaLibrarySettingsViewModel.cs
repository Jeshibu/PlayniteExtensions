using EaLibrary.Services;
using Playnite.SDK;
using System;

namespace EaLibrary;

public class EaLibrarySettings
{
    public int Version { get; set; }
    public bool ImportInstalledGames { get; set; } = true;
    public bool ConnectAccount { get; set; } = false;
    public bool ImportUninstalledGames { get; set; } = false;
}

public class EaLibrarySettingsViewModel : PluginSettingsViewModel<EaLibrarySettings, EaLibrary>
{
    public bool IsUserLoggedIn
    {
        get
        {
            using (var view = PlayniteApi.WebViews.CreateOffscreenView())
            {
                var api = new OriginAccountClient(view);
                return api.GetIsUserLoggedIn();
            }
        }
    }

    public RelayCommand<object> LoginCommand
    {
        get => new RelayCommand<object>((a) =>
        {
            Login();
        });
    }

    public EaLibrarySettingsViewModel(EaLibrary library, IPlayniteAPI api) : base(library, api)
    {
        var savedSettings = LoadSavedSettings();
        if (savedSettings != null)
        {
            if (savedSettings.Version == 0)
            {
                Logger.Debug("Updating Origin settings from version 0.");
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

    private void Login()
    {
        try
        {
            using (var view = PlayniteApi.WebViews.CreateView(490, 670))
            {
                var api = new OriginAccountClient(view);
                api.Login();
            }

            OnPropertyChanged(nameof(IsUserLoggedIn));
        }
        catch (Exception e) when (!Environment.IsDebugBuild)
        {
            Logger.Error(e, "Failed to authenticate user.");
        }
    }
}
