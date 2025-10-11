using Microsoft.Win32;
using NSubstitute;
using PlayniteExtensions.Common;
using System.IO;
using Xunit;

namespace EaLibrary.Tests;

public class RegistryTests
{
    [Fact]
    public void InstallPathResolvesCorrectly()
    {
        const string expected = @"C:\Program Files\EA Games\Bejeweled 3\";
        var registry = Substitute.For<IRegistryValueProvider>();
        registry.GetValueForPath(RegistryHive.LocalMachine, @"SOFTWARE\PopCap\Bejeweled 3", "Install Dir").Returns(expected);
        
        var lib = new EaLibraryDataGatherer(null, registry, null, Path.GetTempPath());
        var installDir = lib.GetInstallDirectory(@"[HKEY_LOCAL_MACHINE\SOFTWARE\PopCap\Bejeweled 3\Install Dir]Bejeweled3.exe");
        
        Assert.Equal(expected, installDir.InstallDirectory);
        Assert.Equal("Bejeweled3.exe", installDir.RelativeFilePath);
    }
}