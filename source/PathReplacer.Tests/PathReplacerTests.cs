using Playnite.SDK.Models;
using Xunit;

namespace PathReplacer.Tests;

public class PathReplacerTests
{
    private Game GetTestGame()
    {
        return new Game("Test Game")
        {
            InstallDirectory = @"D:\Emulation\ROMs\PS2\",
            Roms =
            [
                new GameRom("1", @"D:\Emulation\ROMs\PS2\disc1.bin"),
                new GameRom("2", "D:/Emulation/ROMs/PS2/disc2.bin"),
            ]
        };
    }

    [Fact]
    public void ReplacesInAllPaths()
    {
        var game = GetTestGame();

        var update = PathReplacer.ReplacePathsForGame(game, @"D:\Emulation\", @"X:\");
        Assert.True(update);
        Assert.Equal(@"X:\ROMs\PS2\", game.InstallDirectory);
        Assert.Equal(@"X:\ROMs\PS2\disc1.bin", game.Roms[0].Path);
        Assert.Equal(@"X:\ROMs/PS2/disc2.bin", game.Roms[1].Path);
    }

    [Fact]
    public void DoesNotReplaceWhenUnnecessary()
    {
        var game = GetTestGame();

        var update = PathReplacer.ReplacePathsForGame(game, @"C:\Program Files\", @"X:\");

        var expected = GetTestGame();

        Assert.False(update);
        Assert.Equal(expected.InstallDirectory, game.InstallDirectory);
        Assert.Equal(expected.Roms[0].Path, game.Roms[0].Path);
        Assert.Equal(expected.Roms[1].Path, game.Roms[1].Path);
    }

    [Fact]
    public void IsCaseInsensitive()
    {
        var game = GetTestGame();

        var update = PathReplacer.ReplacePathsForGame(game, @"d:\emulation\", @"X:\");
        Assert.True(update);
        Assert.Equal(@"X:\ROMs\PS2\", game.InstallDirectory);
        Assert.Equal(@"X:\ROMs\PS2\disc1.bin", game.Roms[0].Path);
        Assert.Equal(@"X:\ROMs/PS2/disc2.bin", game.Roms[1].Path);
    }
}
