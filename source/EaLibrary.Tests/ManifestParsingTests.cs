using System.IO;
using Xunit;

namespace EaLibrary.Tests;

public class ManifestParsingTests
{
    [Fact]
    public void Sims4()
    {
        var scanner = new EaInstallerDataScanner();
        var dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Sims4"));
        var manifests = scanner.GetManifests(dir);
        
        Assert.Single(manifests);
        Assert.Equal("The Sims™ 4", manifests[0].Name);
        Assert.Equal(dir.FullName, manifests[0].InstallDirectory);
        Assert.Equal(@"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{48EBEBBF-B9F8-4520-A3CF-89A730721917}\UninstallString]", manifests[0].UninstallerPath);
    }
}