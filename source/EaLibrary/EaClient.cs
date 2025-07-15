using Playnite.Common;
using Playnite.SDK;
using System.Diagnostics;
using System.Linq;

namespace EaLibrary;

public class EaClient : LibraryClient
{
    private static readonly ILogger logger = LogManager.GetLogger();

    public override string Icon => Origin.Icon;

    public override bool IsInstalled => Origin.IsInstalled;

    public override void Open()
    {
        Origin.StartClient();
    }

    public override void Shutdown()
    {
        var mainProc = Process.GetProcessesByName("EADesktop").FirstOrDefault();
        if (mainProc == null)
        {
            logger.Info("EA app is no longer running, no need to shut it down.");
            return;
        }

        var procRes = ProcessStarter.StartProcessWait(CmdLineTools.TaskKill, $"/pid {mainProc.Id}", null, out var stdOut, out var stdErr);
        if (procRes != 0)
        {
            logger.Error($"Failed to close EA app: {procRes}, {stdErr}");
        }
    }
}
