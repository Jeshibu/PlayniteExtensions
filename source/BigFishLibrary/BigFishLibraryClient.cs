using Playnite.SDK;
using System.Diagnostics;
using System.IO;

namespace BigFishLibrary;

public class BigFishLibraryClient : LibraryClient
{
    private readonly BigFishRegistryReader registryReader;
    private readonly string iconPath;

    public BigFishLibraryClient(BigFishRegistryReader registryReader, string iconPath)
    {
        this.registryReader = registryReader;
        this.iconPath = iconPath;
    }

    private string ExePath => $@"{registryReader.GetClientInstallDirectory()}\bfgclient.exe";

    public override bool IsInstalled => File.Exists(ExePath);

    public override void Open()
    {
        if (IsInstalled)
            try { Process.Start(ExePath); } catch { }
    }

    public override string Icon => iconPath;
}