using LoadingBayLibrary.Controllers;
using LoadingBayLibrary.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LoadingBayLibrary;

// ReSharper disable once UnusedType.Global
public class LoadingBayLibrary : LibraryPlugin
{
    public override Guid Id { get; } = Guid.Parse("01c16010-a35d-4e66-add4-48211740c21b");

    public override string Name => "Loading Bay";

    public LoadingBayLibrary(IPlayniteAPI api) : base(api)
    {
        Properties = new()
        {
            HasSettings = false,
            CanShutdownClient = false,
        };
    }

    public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
    {
        var registryReader = new RegistryReader(new RegistryValueProvider());
        var website = new Website(new WebDownloader());

        var recommendedGames = website.GetRecommendedGames();
        var installedGames = registryReader.GetInstalledGames().ToDictionary(x => x.Id);

        foreach (var recommendedGame in recommendedGames.data)
        {
            var game = new GameMetadata
            {
                GameId = recommendedGame.app_data.app_id.ToString(),
                Name = recommendedGame.app_data.display_name,
                Platforms = [new MetadataSpecProperty("pc_windows")],
                Source = new MetadataNameProperty("LoadingBay"),
                IsInstalled = false,
            };
            if (installedGames.TryGetValue(game.GameId, out var installedGame))
            {
                game.IsInstalled = true;
                game.InstallDirectory = installedGame.InstallPath;
            }

            yield return game;
        }
    }

    private static RegistryReader Registry => new(new RegistryValueProvider());

    public override LibraryClient Client => new LoadingBayClient(Registry);

    public override IEnumerable<InstallController> GetInstallActions(GetInstallActionsArgs args)
    {
        if (args.Game.PluginId == Id)
            yield return new LoadingBayInstallController(args.Game, Registry, PlayniteApi.Dialogs);
    }

    public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
    {
        if (args.Game.PluginId == Id)
            yield return new LoadingBayUninstallController(args.Game, Registry, PlayniteApi.Dialogs);
    }

    public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
    {
        if (args.Game.PluginId != Id)
            yield break;

        yield return new AutomaticPlayController(args.Game)
        {
            Type = AutomaticPlayActionType.Url,
            Path = ControllerHelper.GetLoadingBayGamePageUrl(args.Game),
            TrackingMode = TrackingMode.Directory,
            TrackingPath = args.Game.InstallDirectory,
            Name = "Start via LoadingBay",
        };
    }
}
