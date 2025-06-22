using Xunit;

namespace ViveportLibrary.Tests;

public class ViveportLibraryTests
{
    [Theory]
    [InlineData("HtcViveCosmosElite", "HTC Vive Cosmos Elite")]
    [InlineData("OculusRiftS", "Oculus Rift S")]
    public void SplitPascalCaseTest(string input, string expectedOutput)
    {
        var output = ViveportLibrary.SplitPascalCase(input);
        Assert.Equal(expectedOutput, output);
    }
}
