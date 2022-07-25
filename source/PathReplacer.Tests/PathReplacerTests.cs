using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PathReplacer.Tests
{
    public class PathReplacerTests
    {
        [Fact]
        public void Works()
        {
            Game game = new Game("Test Game")
            {
                InstallDirectory = @"D:\Emulation\ROMs\PS2\",
                Roms = new System.Collections.ObjectModel.ObservableCollection<GameRom>
                {
                    new GameRom("1", @"D:\Emulation\ROMs\PS2\disc1.bin"),
                    new GameRom("2", @"D:/Emulation/ROMs/PS2/disc2.bin"),
                }
            };

            var update = PathReplacer.ReplacePathsForGame(game, @"D:\Emulation\", @"X:\");
            Assert.True(update);
            Assert.Equal(@"X:\ROMs\PS2\", game.InstallDirectory);
            Assert.Equal(@"X:\ROMs\PS2\disc1.bin", game.Roms[0].Path);
            Assert.Equal(@"X:\ROMs/PS2/disc2.bin", game.Roms[1].Path);
        }
    }
}
