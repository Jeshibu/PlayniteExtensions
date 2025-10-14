using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EaLibrary.ActionControllers;

public class EaUninstallController : UninstallController
{
    private static readonly ILogger logger = LogManager.GetLogger();
    private CancellationTokenSource _watcherToken;
    private readonly EaLibrary _library;

    public EaUninstallController(Game game, EaLibrary library) : base(game)
    {
        _library = library;
        Name = "Uninstall using EA app";
    }

    public override void Dispose()
    {
        _watcherToken?.Cancel();
    }

    public override void Uninstall(UninstallActionArgs args)
    {
        Dispose();

        Task.Run(StartUninstallWatcher);
    }

    private async Task StartUninstallWatcher()
    {
        try
        {
            ProcessStarter.StartUrl(EaApp.LibraryOpenUri);
            
            _watcherToken = new();

            while (true)
            {
                if (_watcherToken.IsCancellationRequested)
                    return;

                try
                {
                    var status = await _library.DataGatherer.GetGameInstallationStatusAsync(Game.GameId);
                    if (!status.IsInstalled)
                    {
                        InvokeOnUninstalled(new());
                        return;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error while installing EA game {Game.GameId})");
                    return;
                }

                await Task.Delay(2000);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error while uninstalling EA game {Game.Name} ({Game.GameId})");
        }
    }
}
