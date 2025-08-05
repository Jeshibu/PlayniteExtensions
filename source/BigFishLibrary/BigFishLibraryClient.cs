using Playnite.SDK;
using System.Diagnostics;
using System.IO;

namespace BigFishLibrary;

public class BigFishLibraryClient(BigFishRegistryReader registryReader, string iconPath) : LibraryClient
{
    private string ExePath => $@"{registryReader.GetClientInstallDirectory()}\bfgclient.exe";

    public override bool IsInstalled => File.Exists(ExePath);

    public override void Open()
    {
        if (IsInstalled)
            try { Process.Start(ExePath); } catch { }
    }

    public override string Icon => iconPath;
}