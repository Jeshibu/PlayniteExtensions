using PlayniteExtensions.Common;
using System.IO;
using Xunit;

namespace LegacyGamesLibrary.Tests;

public class RegistryReaderTests
{
    //[Fact]
    public void CanFindPath()
    {
        var reg = new LegacyGamesRegistryReader(new RegistryValueProvider());
        string path = reg.GetLauncherPath();
        Assert.NotNull(path);
        Assert.True(File.Exists(path));
    }
}
