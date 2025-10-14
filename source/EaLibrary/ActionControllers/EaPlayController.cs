using EaLibrary.Models;
using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EaLibrary.ActionControllers;

public class EaPlayController : PlayController
{
    private static ILogger logger = LogManager.GetLogger();
    private ProcessMonitor procMon;
    private Stopwatch stopWatch;
    private readonly EaLibrary eaLibrary;

    public EaPlayController(Game game, EaLibrary library) : base(game)
    {
        Name = string.Format(ResourceProvider.GetString(LOC.OriginStartUsingClient), "EA app");
        eaLibrary = library;
    }

    public override void Play(PlayActionArgs args)
    {
        Dispose();
        
        Task.Run(PlayAsync);
    }
    
    public override void Dispose()
    {
        procMon?.Dispose();
    }

    private async Task PlayAsync()
    {
        try
        {
            if (!Directory.Exists(Game.InstallDirectory))
            {
                var error = $"Game install directory doesn't exist: {Game.InstallDirectory}";
                logger.Warn(error);
                eaLibrary.PlayniteApi.Notifications.Add($"ea-launch-{Game.GameId}-failed", error, NotificationType.Error);
                InvokeOnStopped(new());
                return;
            }

            var legacyOffer = await EaControllerHelper.LaunchGame(Game, logger, eaLibrary);
            if (legacyOffer == null)
            {
                InvokeOnStopped(new());
                return;
            }

            procMon = new ProcessMonitor();
            procMon.TreeDestroyed += ProcMon_TreeDestroyed;
            procMon.TreeStarted += ProcMon_TreeStarted;

            // Solves issues with game process being started/shutdown multiple times during startup via Origin
            var delaySeconds = EaApp.GetGameUsesEasyAntiCheat(Game.InstallDirectory) ? 40 : 2;

            procMon.WatchDirectoryProcesses(Game.InstallDirectory, false, trackingDelay: delaySeconds * 1000);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error while running EA game {Game.Name} ({Game.GameId})");
        }
    }

    private void ProcMon_TreeStarted(object sender, ProcessMonitor.TreeStartedEventArgs args)
    {
        stopWatch = Stopwatch.StartNew();
        InvokeOnStarted(new GameStartedEventArgs() { StartedProcessId = args.StartedId });
    }

    private void ProcMon_TreeDestroyed(object sender, EventArgs args)
    {
        stopWatch?.Stop();
        InvokeOnStopped(new GameStoppedEventArgs(Convert.ToUInt64(stopWatch?.Elapsed.TotalSeconds ?? 0)));
    }
}
