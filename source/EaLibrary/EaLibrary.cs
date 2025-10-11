using EaLibrary.ActionControllers;
using EaLibrary.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EaLibrary;

[LoadPlugin]
public class EaLibrary : LibraryPluginBase<EaLibrarySettingsViewModel>
{
    private static readonly ILogger logger = LogManager.GetLogger();
    private IWebDownloader _downloader;
    private PlatformUtility _platformUtility;
    private IEaWebsite _website;
    private EaLibraryDataGatherer _dataGatherer;
    private IWebDownloader Downloader => _downloader ??= new WebDownloader();
    private IPlatformUtility PlatformUtility => _platformUtility ??= new();
    public IEaWebsite Website => _website ??= new EaWebsite(PlayniteApi.WebViews, Downloader);
    public EaLibraryDataGatherer DataGatherer => _dataGatherer ??= new(Website, new RegistryValueProvider(), PlatformUtility, GetPluginUserDataPath());

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
                                              System.Environment.NewLine + importError.Message,
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
        if (args.Game.PluginId != Id)
            yield break;

        var legacyOffer = DataGatherer.GetLegacyOffer(args.Game.GameId);
        if (string.IsNullOrWhiteSpace(legacyOffer?.contentId))
        {
            logger.Warn($"No content ID found for game {args.Game.GameId} ({args.Game.Name})");
            yield break;
        }
        
        logger.Info($"Starting EA content {legacyOffer.contentId} ({args.Game.Name})");

        var installDir = DataGatherer.GetInstallDirectory(legacyOffer.installCheckOverride).InstallDirectory;

        yield return new AutomaticPlayController(args.Game)
        {
            Type = AutomaticPlayActionType.Url,
            Path = "origin2://game/launch/?offerIds=" + legacyOffer.contentId,
            TrackingPath = installDir,
            TrackingMode = TrackingMode.Directory,
            Name = $"EA: {legacyOffer.displayName}",
            InitialTrackingDelay = 40_000,
        };
    }
}
