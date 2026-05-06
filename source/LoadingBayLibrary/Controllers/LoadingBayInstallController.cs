using LoadingBayLibrary.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LoadingBayLibrary.Controllers;

public class LoadingBayInstallController(Game game, RegistryReader registry, IDialogsFactory dialogs) : InstallController(game)
{
    private CancellationTokenSource watcherToken;
    private readonly ILogger logger = LogManager.GetLogger();

    public override void Dispose() => watcherToken?.Cancel();

    public override void Install(InstallActionArgs args)
    {
        if (ControllerHelper.OpenGamePage(Game, registry, dialogs))
            _ = StartInstallWatcher();
    }

    private async Task StartInstallWatcher()
    {
        watcherToken = new();

        while (true)
        {
            if (watcherToken.IsCancellationRequested)
                return;

            try
            {
                var installedGames = registry.GetInstalledGames();
                var installedGame = installedGames?.FirstOrDefault(g => g.Id == Game.GameId);
                if (installedGame?.InstallPath != null)
                {
                    InvokeOnInstalled(new(new GameInstallationData { InstallDirectory = installedGame.InstallPath }));
                    return;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get info about installed LoadingBay games.");
            }

            await Task.Delay(5000);
        }
    }
}