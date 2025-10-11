using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.IO;
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
        StartInstallWatcher();
    }

    public async void StartInstallWatcher()
    {
        _watcherToken = new CancellationTokenSource();
        var manifest = _library.DataGatherer.GetLegacyOffer(Game.GameId);
        if (manifest?.installCheckOverride == null)
        {
            logger.Error($"No install check found for EA game {Game.GameId}, stopping installation check.");
            return;
        }

        await Task.Run(async () =>
        {
            while (true)
            {
                if (_watcherToken.IsCancellationRequested)
                {
                    return;
                }

                var installData = _library.DataGatherer.GetInstallDirectory(manifest.installCheckOverride);
                var executablePath = Path.Combine(installData.InstallDirectory, installData.RelativeFilePath);

                if (File.Exists(executablePath))
                {
                    InvokeOnInstalled(new(new() { InstallDirectory = installData.InstallDirectory }));
                    return;
                }

                await Task.Delay(10000);
            }
        });
    }
}