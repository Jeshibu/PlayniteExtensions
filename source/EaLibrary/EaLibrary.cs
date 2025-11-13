using EaLibrary.ActionControllers;
using EaLibrary.Services;
using EaLibrary.Settings;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EaLibrary;

// ReSharper disable once ClassNeverInstantiated.Global
public class EaLibrary : LibraryPluginBase<EaLibrarySettingsViewModel>
{
    private static readonly ILogger logger = LogManager.GetLogger();
    private PlatformUtility _platformUtility;
    private IWebDownloader Downloader => field ??= new WebDownloader();
    private IPlatformUtility PlatformUtility => _platformUtility ??= new();
    public IEaWebsite Website => field ??= new EaWebsite(PlayniteApi.WebViews, Downloader);
    public EaLibraryDataGatherer DataGatherer => field ??= new(Website, new RegistryValueProvider(), PlatformUtility, GetPluginUserDataPath());

    public EaLibrary(IPlayniteAPI api) : base(
        "EA app",
        Guid.Parse("85DD7072-2F20-4E76-A007-41035E390724"),
        new LibraryPluginProperties { CanShutdownClient = true, HasSettings = true },
        new EaClient(),
        EaApp.Icon,
        _ => new EaLibrarySettingsView(),
        api)
    {
        SettingsViewModel = new EaLibrarySettingsViewModel(this, PlayniteApi);
    }

    public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
    {
        if (!SettingsViewModel.Settings.ConnectAccount)
            return [];

        var allGames = new List<GameMetadata>();
        Exception importError = null;

        try
        {
            allGames = DataGatherer.GetGames().ToList();
            Logger.Debug($"Found {allGames.Count} library EA games.");
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to import linked account EA games details.");
            importError = e;
        }

        if (importError == null)
        {
            if (!SettingsViewModel.Settings.ImportUninstalledGames)
            {
                allGames.RemoveAll(a => !a.IsInstalled);
            }
        }

        if (importError != null)
        {
            PlayniteApi.Notifications.Add(new NotificationMessage(
                                              ImportErrorMessageId,
                                              string.Format(PlayniteApi.Resources.GetString("LOCLibraryImportError"), Name) +
                                              Environment.NewLine + importError.Message,
                                              NotificationType.Error,
                                              () => OpenSettingsView()));
        }
        else
        {
            PlayniteApi.Notifications.Remove(ImportErrorMessageId);
        }

        return allGames;
    }

    public override IEnumerable<InstallController> GetInstallActions(GetInstallActionsArgs args)
    {
        if (args.Game.PluginId == Id)
            yield return new EaInstallController(args.Game, this);
    }

    public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
    {
        if (args.Game.PluginId == Id)
            yield return new EaUninstallController(args.Game, this);
    }

    public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
    {
        if (args.Game.PluginId == Id)
            yield return new EaPlayController(args.Game, this);
    }
}
