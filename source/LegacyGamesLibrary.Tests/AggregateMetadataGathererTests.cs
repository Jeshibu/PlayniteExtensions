using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LegacyGamesLibrary.Tests
{
    public class AggregateMetadataGathererTests
    {
        [Theory]
        [InlineData("277.4 MB", 277.4D * 1024 * 1024)]
        [InlineData("1.4 GB", 1.4D * 1024 * 1024 * 1024)]
        public void TestInstallSizeParsing(string str, double expected)
        {
            var result = AggregateMetadataGatherer.ParseInstallSizeString(str);
            var e = Convert.ToUInt64(expected);
            Assert.Equal(e, result);
        }
    }
}
