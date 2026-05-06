using LoadingBayLibrary.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LoadingBayLibrary.Controllers;

public class LoadingBayUninstallController(Game game, RegistryReader registry, IDialogsFactory dialogs) : UninstallController(game)
{
    private CancellationTokenSource watcherToken;
    private readonly ILogger logger = LogManager.GetLogger();

    public override void Dispose() => watcherToken?.Cancel();

    public override void Uninstall(UninstallActionArgs args)
    {
        if (ControllerHelper.OpenGamePage(Game, registry, dialogs))
            _ = StartUninstallWatcher();
    }

    private async Task StartUninstallWatcher()
    {
        watcherToken = new();

        while (true)
        {
            if (watcherToken.IsCancellationRequested)
                return;

            try
            {
                var installedGames = registry.GetInstalledGames();
                if (installedGames != null && installedGames.All(x => x.Id != Game.GameId))
                {
                    InvokeOnUninstalled(new());
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