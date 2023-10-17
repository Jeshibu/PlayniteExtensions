using Playnite.SDK.Models;
using System.Linq;
using Xunit;

namespace PlayniteExtensions.Common.Tests
{
    public class PlatformUtilityTests
    {
        [Theory]
        [InlineData("Sony PSP", "sony_psp")]
        public static void MatchSinglePlatformDefinition(string input, string expectedPlatformDefinition)
        {
            var platformUtility = new PlatformUtility((string)null);

            var platforms = platformUtility.GetPlatforms(input);

            Assert.Single(platforms);
            var platform = platforms.Single();
            Assert.IsType<MetadataSpecProperty>(platform);
            var specPlatform = (MetadataSpecProperty)platform;
            Assert.Equal(expectedPlatformDefinition, specPlatform.Id);
        }
    }
}
