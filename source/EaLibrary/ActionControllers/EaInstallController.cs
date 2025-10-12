using Playnite.Common;
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

    public override void Dispose()
    {
        _watcherToken?.Cancel();
    }

    public override void Install(InstallActionArgs args)
    {
        Dispose();
        ProcessStarter.StartUrl(EaApp.LibraryOpenUri);
        
        #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        StartInstallWatcher();
    }

    private async Task StartInstallWatcher()
    {
        _watcherToken = new();
        
        await Task.Run(async () =>
        {
            while (true)
            {
                if (_watcherToken.IsCancellationRequested)
                    return;

                try
                {
                    if (_library.DataGatherer.GameIsInstalled(Game.GameId, out string installDirectory))
                    {
                        InvokeOnInstalled(new(new() { InstallDirectory = installDirectory }));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error while installing EA game {Game.GameId})");
                    return;
                }

                await Task.Delay(10000);
            }
        });
    }
}
