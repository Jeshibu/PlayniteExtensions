using Playnite.SDK.Models;
using System.Linq;
using Xunit;

namespace PlayniteExtensions.Common.Tests;

public class PlatformUtilityTests
{
    [Theory]
    [InlineData("Sony PSP", "sony_psp")]
    [InlineData("Sony Playstation Portable", "sony_psp")]
    [InlineData("Sony Playstation 2", "sony_playstation2")]
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
