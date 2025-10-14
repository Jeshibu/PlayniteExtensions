using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EaLibrary.ActionControllers;

public class EaInstallController : InstallController
{
    private static readonly ILogger logger = LogManager.GetLogger();
    private CancellationTokenSource _watcherToken;
    private readonly EaLibrary _library;

    public EaInstallController(Game game, EaLibrary library) : base(game)
    {
        _library = library;
        Name = "Install using EA app";
    }

    public override void Install(InstallActionArgs args)
    {
        Dispose();

        Task.Run(StartInstallWatcher);
    }

    public override void Dispose()
    {
        _watcherToken?.Cancel();
    }

    private async Task StartInstallWatcher()
    {
        try
        {
            _watcherToken = new();

            // opens install dialog for uninstalled games
            await EaControllerHelper.LaunchGame(Game, logger, _library);

            while (true)
            {
                if (_watcherToken.IsCancellationRequested)
                    return;

                var status = await _library.DataGatherer.GetGameInstallationStatusAsync(Game.GameId);
                if (status.IsInstalled)
                {
                    InvokeOnInstalled(new(new() { InstallDirectory = status.InstallDirectory }));
                    return;
                }

                await Task.Delay(10000);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error while installing EA game {Game.Name} ({Game.GameId})");
        }
    }
}
