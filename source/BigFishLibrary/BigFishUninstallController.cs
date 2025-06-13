using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace BigFishLibrary;

public class BigFishUninstallController : UninstallController
{
    private readonly BigFishRegistryReader registryReader;
    private readonly string uninstallerPath;
    private CancellationTokenSource watcherToken;
    private readonly ILogger logger = LogManager.GetLogger();

    public BigFishUninstallController(Game game, BigFishRegistryReader registryReader, string uninstallerPath) : base(game)
    {
        this.registryReader = registryReader;
        this.uninstallerPath = uninstallerPath;
    }

    public override void Uninstall(UninstallActionArgs args)
    {
        System.Diagnostics.Process.Start(uninstallerPath);
        StartUninstallWatcher();
    }

    public async void StartUninstallWatcher()
    {
        watcherToken = watcherToken ?? new CancellationTokenSource();

        while (true)
        {
            if (watcherToken.IsCancellationRequested)
            {
                return;
            }

            string[] installedGameIds = null;
            try
            {
                installedGameIds = registryReader.GetInstalledGameIds();
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get info about installed Big Fish games.");
            }

            if (installedGameIds != null)
            {
                if (!installedGameIds.Contains(Game.GameId))
                {
                    InvokeOnUninstalled(new GameUninstalledEventArgs());
                    return;
                }
            }

            await Task.Delay(2000);
        }
    }
}