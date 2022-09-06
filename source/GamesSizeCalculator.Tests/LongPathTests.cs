using PluginsCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GamesSizeCalculator.Tests
{
    public class LongPathTests
    {
        [Fact]
        public void LongPathCanBeFileSizeCalculated()
        {
            string dir = FileSystem.FixPathLength(Path.Combine(Directory.GetCurrentDirectory(), @"LongPath\USHF-438466\Ultimate Sonic Heroes Fighters - Definitive Edition-692434\data\USHF\Ultimate Sonic Heroes Fighters - Definitive Edition\reshade-shaders\Shaders\CorgiFX\StageDepthPlus with depth buffer modification"));
            string filePath = Path.Combine(dir, "StageDepthPlus with depth buffer modification.txt");
            Directory.CreateDirectory(dir);
            using (var stream = File.CreateText(filePath))
            {
                stream.WriteLine("This is a file to test long paths.");
            }

            var size = FileSystem.GetDirectorySizeOnDisk("./LongPath");
            Assert.NotEqual(0, size);
        }
    }
}
