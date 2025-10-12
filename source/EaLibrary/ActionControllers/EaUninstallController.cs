using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
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
        ProcessStarter.StartUrl(EaApp.LibraryOpenUri);
        
        #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        StartUninstallWatcher();
    }

    private async Task StartUninstallWatcher()
    {
        _watcherToken = new();

        await Task.Run(async () =>
        {
            while (true)
            {
                if (_watcherToken.IsCancellationRequested)
                    return;

                if (!_library.DataGatherer.GameIsInstalled(Game.GameId, out _))
                {
                    InvokeOnUninstalled(new());
                    return;
                }

                await Task.Delay(2000);
            }
        });
    }
}