using LoadingBayLibrary.Services;
using Playnite.SDK;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace LoadingBayLibrary;

public class LoadingBayClient(RegistryReader registry) : LibraryClient
{
    private readonly ILogger logger = LogManager.GetLogger();
    public override string Icon => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "icon.png");
    public override bool IsInstalled => File.Exists(registry.GetClientPath());

    public override void Open()
    {
        try
        {
            Process.Start(registry.GetClientPath());
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error while opening LoadingBay client");
        }
    }
}
