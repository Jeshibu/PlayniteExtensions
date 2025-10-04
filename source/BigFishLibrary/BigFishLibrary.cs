using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Reflection;
using BigFishMetadata;

namespace BigFishLibrary;

// ReSharper disable once ClassNeverInstantiated.Global
public class BigFishLibrary : LibraryPlugin
{
    private static readonly string iconPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "icon.png");

    private static readonly ILogger logger = LogManager.GetLogger();

    private BigFishLibrarySettingsViewModel settings { get; set; }

    public override Guid Id { get; } = Guid.Parse("37995df7-2ce2-4f7c-83a3-618138ae745d");

    public const string PluginName = "Big Fish Games";

    public override string Name => PluginName;

    public override LibraryClient Client => new BigFishLibraryClient(RegistryReader, iconPath);

    public override string LibraryIcon => iconPath;

    private BigFishRegistryReader RegistryReader { get; }

    private IWebDownloader Downloader { get; }

    private BigFishMetadataProvider MetadataProvider
    {
        get
        {
            var searchProvider = new BigFishSearchProvider(Downloader, settings.Settings);
            var scraper = new BigFishOnlineLibraryScraper(PlayniteApi, Downloader);
            return new BigFishMetadataProvider(RegistryReader, searchProvider, settings.Settings, scraper);
        }
    }

    public BigFishLibrary(IPlayniteAPI api) : base(api)
    {
        settings = new BigFishLibrarySettingsViewModel(this);
        Properties = new LibraryPluginProperties
        {
            HasSettings = false
        };
        RegistryReader = new BigFishRegistryReader(new RegistryValueProvider());
        Downloader = new WebDownloader { Accept = "*/*" };
    }

    public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
    {
        try
        {
            SetUninstalledStatus();

            var metadataProvider = MetadataProvider;
            var onlineGames = metadataProvider.GetOnlineGames().ToDictionary(g => g.GameId);
            logger.Info($"Found {onlineGames.Count} online games");
            
            var offlineGames = metadataProvider.GetOfflineGames().ToList();
            logger.Info($"Found {offlineGames.Count} offline games");
            
            foreach (var offlineGame in offlineGames)
            {
                if (onlineGames.TryGetValue(offlineGame.GameId, out var onlineGame))
                    onlineGames[offlineGame.GameId] = BigFishMetadataProvider.Merge(offlineGame, onlineGame);
                else
                    onlineGames[offlineGame.GameId] = offlineGame;
            }
            return onlineGames.Values;
        }
        catch (NotAuthenticatedException)
        {
            logger.Error("Not authenticated");
            PlayniteApi.Notifications.Add(new("bigfish-notauthenticated", "Big Fish library isn't authenticated. Click here to authenticate.", NotificationType.Info, () => OpenSettingsView()));
            return [];
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting games");
            PlayniteApi.Notifications.Add("bigfish-error", $"Big Fish library error: {ex.Message}", NotificationType.Error);
            return [];
        }
    }

    private void SetUninstalledStatus()
    {
        try
        {
            foreach (var game in PlayniteApi.Database.Games.Where(g => g.PluginId == Id && g.IsInstalled))
            {
                if (string.IsNullOrEmpty(game.InstallDirectory))
                {
                    game.IsInstalled = false;
                    PlayniteApi.Database.Games.Update(game);
                    continue;
                }

                var dir = new DirectoryInfo(game.InstallDirectory);
                if (!dir.Exists || !dir.EnumerateFileSystemInfos().Any())
                {
                    game.IsInstalled = false;
                    PlayniteApi.Database.Games.Update(game);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error setting uninstalled status");
        }
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new BigFishLibrarySettingsView();
    }

    public override LibraryMetadataProvider GetMetadataDownloader() => MetadataProvider;

    public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
    {
        if (args.Game.PluginId != Id)
            yield break;

        var installData = RegistryReader.GetGameDetails(args.Game.GameId);

        if (installData == null || string.IsNullOrWhiteSpace(installData.ExecutablePath))
        {
            logger.Debug($"No install data found for {args.Game.Name}, ID: {args.Game.GameId}");
            PlayniteApi.Dialogs.ShowErrorMessage("No install data found.", "Big Fish Games launch error");
        }

        var directory = new FileInfo(installData.ExecutablePath).Directory;
        var files = directory.GetFiles("*.exe")
            .Where(f =>
                !f.Name.Equals("uninstall.exe", StringComparison.InvariantCultureIgnoreCase)
                && !f.FullName.Equals(installData.ExecutablePath, StringComparison.InvariantCultureIgnoreCase)
            ).ToArray();

        if (files.Length != 1)
            yield break;

        yield return new AutomaticPlayController(args.Game)
        {
            Path = files.Single().FullName,
            TrackingMode = TrackingMode.Default
        };
    }

    public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
    {
        if (args.Game.PluginId != Id)
            yield break;

        var installData = RegistryReader.GetGameDetails(args.Game.GameId);

        if (installData == null || string.IsNullOrWhiteSpace(installData.ExecutablePath))
        {
            logger.Debug($"No install data found for {args.Game.Name}, ID: {args.Game.GameId}");
            PlayniteApi.Dialogs.ShowErrorMessage("No install data found.", "Big Fish Games launch error");
        }

        var directory = new FileInfo(installData.ExecutablePath).Directory;
        var files = directory.GetFiles("*.exe")
            .Where(f => f.Name.Equals("uninstall.exe", StringComparison.InvariantCultureIgnoreCase)).ToArray();

        if (files.Length != 1)
        {
            yield break;
        }

        yield return new BigFishUninstallController(args.Game, RegistryReader, files[0].FullName);
    }
}