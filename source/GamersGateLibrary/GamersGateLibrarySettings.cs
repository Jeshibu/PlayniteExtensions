using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows.Media;

namespace GamersGateLibrary;

public class GamersGateLibrarySettings : ObservableObject
{
    public Dictionary<string, GameInstallInfo> InstallData{ get; set => SetValue(ref field, value); } = [];
    public bool UseCoverImages{ get; set => SetValue(ref field, value); } = true;
    public int MinimumWebRequestDelay{ get; set => SetValue(ref field, value); } = 2000;
    public int MaximumWebRequestDelay{ get; set => SetValue(ref field, value); } = 4000;
    public OnImportAction ImportAction{ get; set => SetValue(ref field, value); } = OnImportAction.ImportOffscreen;
    public HashSet<int> KnownOrderIds{ get; set => SetValue(ref field, value); } = [];
    public int Version { get; set; } = 1;

    public void ClearKnownOrderIds()
    {
        KnownOrderIds.Clear();
        OnPropertyChanged(nameof(KnownOrderIds));
    }
}

public class GameInstallInfo
{
    public string Id { get; set; }
    public int OrderId { get; set; }
    public string Name { get; set; }
    public List<DownloadUrl> DownloadUrls { get; set; } = [];
    public string InstallLocation { get; set; }
    public string RelativeExecutablePath { get; set; }
    public bool UnrevealedKey { get; set; }
    public string Key { get; set; }
    public string DRM { get; set; }
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

public enum OnImportAction
{
    [Description("Ask for confirmation")]
    Prompt = 0,

    [Description("Do nothing")]
    DoNothing = 1,

    [Description("Import without asking")]
    ImportWithoutPrompt = 2,

    [Description("Import offscreen")]
    ImportOffscreen = 3,
}

public class GamersGateLibrarySettingsViewModel : PluginSettingsViewModel<GamersGateLibrarySettings, GamersGateLibrary>
{
    public AuthStatus AuthStatus
    {
        get
        {
            var view = PlayniteApi.WebViews.CreateOffscreenView();
            try
            {
                string profileUrl = "https://www.gamersgate.com/account/settings/";
                view.NavigateAndWait(profileUrl);
                string actualUrl = view.GetCurrentAddress(); //this will be a login URL if not authenticated

                if (actualUrl == profileUrl)
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
                view.Dispose();
            }
        }
    }

    public GamersGateLibrarySettingsViewModel(GamersGateLibrary plugin, IPlayniteAPI playniteAPI) : base(plugin, playniteAPI)
    {
        Settings = LoadSavedSettings() ?? new GamersGateLibrarySettings { Version = CurrentVersion };

        UpgradeSettings();
    }

    public RelayCommand<object> LoginCommand => new(_ => Login());

    public RelayCommand<object> ClearKnownOrderIdsCommand => new(_ => Settings.ClearKnownOrderIds());

    private void Login()
    {
        try
        {
            string loginUrl = "https://www.gamersgate.com/login/";
            var cookies = new List<Cookie>();
            using (var view = PlayniteApi.WebViews.CreateView(675, 540, Colors.Black))
            {
                view.LoadingChanged += async (_, _) =>
                {
                    try
                    {
                        var address = view.GetCurrentAddress();
                        if (!address.StartsWith(loginUrl))
                        {
                            var source = await view.GetPageSourceAsync();
                            if (WebViewWrapper.IsAuthenticated(source))
                            {
                                cookies.AddRange(view.GetCookies().Where(c => c.Domain.EndsWith("gamersgate.com")).Select(PlayniteConvert.ToCookie).ToList());
                                view.Close();
                            }
                        }
                    }
                    catch (Exception ex)
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

            OnPropertyChanged(nameof(AuthStatus));
        }
        catch (Exception e)
        {
            PlayniteApi.Dialogs.ShowErrorMessage("Error logging in to GamersGate", "");
            Logger.Error(e, "Failed to authenticate user.");
        }
    }

    public override bool VerifySettings(out List<string> errors)
    {
        errors = [];
        if (Settings.MinimumWebRequestDelay < 0)
            errors.Add("Minimum web request delay can't be less than 0");

        if (Settings.MaximumWebRequestDelay < 0)
            errors.Add("Maximum web request delay can't be less than 0");

        if (Settings.MinimumWebRequestDelay > Settings.MaximumWebRequestDelay)
            errors.Add("Minimum web request delay can't be less than the maximum");

        return errors.Count == 0;
    }

    private readonly int CurrentVersion = 2;

    private void UpgradeSettings()
    {
        if (Settings.Version == CurrentVersion) return;

        if (Settings.Version < 2 && Settings.InstallData.Count > 0 && Settings.KnownOrderIds.Count == 0)
        {
            Settings.KnownOrderIds = Settings.InstallData.Values.Select(x => x.OrderId).ToHashSet();
            Settings.ImportAction = OnImportAction.ImportOffscreen;
        }

        Settings.Version = CurrentVersion;
        Plugin.SavePluginSettings(Settings);
    }
}
