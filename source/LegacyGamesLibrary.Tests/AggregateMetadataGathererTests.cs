using PlayniteExtensions.Common;
using System;
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
            var result = StringExtensions.ParseInstallSize(str);
            var e = Convert.ToUInt64(expected);
            Assert.Equal(e, result);
        }
    }
}
