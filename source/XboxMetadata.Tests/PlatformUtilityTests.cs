using Playnite.SDK;
using PlayniteExtensions.Common;
using Xunit;

namespace XboxMetadata.Tests;

public class PlatformUtilityTests
{
    [Theory]
    [InlineData("Normal Title - PC", "Normal Title")]
    [InlineData("Title - With Dash - Xbox Series S", "Title - With Dash")]
    [InlineData("Title - With Dash (Xbox Series X)", "Title - With Dash")]
    [InlineData("Title - With Dash (PC Version)", "Title - With Dash")]
    public void StripsPlatforms(string input, string expectedOutput)
    {
        var platformUtility = new PlatformUtility((IPlayniteAPI)null);
        platformUtility.GetPlatformsFromName(input, out string output);
        Assert.Equal(expectedOutput, output);
    }
}
