using FluentAssertions;
using Playnite.SDK.Models;
using System;
using System.Linq;
using Xunit;

namespace PCGamingWikiMetadata.Tests;

public class PCGWGame_Test_LMS : IDisposable
{
    private readonly PCGWGame testGame;
    private readonly LocalPCGWClient client;

    public PCGWGame_Test_LMS()
    {
        this.client = new LocalPCGWClient();
        this.testGame = new PCGWGame(this.client.GetSettings(), "Lawn Mowing Simulator", -1);
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestParseWindowsReleaseDate()
    {
        var date = this.testGame.WindowsReleaseDate();
        Assert.Equal(new ReleaseDate(2021, 8, 10), date);
    }

    [Fact]
    public void TestParseDevelopers()
    {
        var arr = this.testGame.Developers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Skyhook Games");
    }

    [Fact]
    public void TestParsePublishers()
    {
        var arr = this.testGame.Publishers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Curve Digital");
    }

    [Fact]
    public void TestParseModes()
    {
        var arr = this.testGame.Features.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Singleplayer");
    }

    [Fact]
    public void TestControllerSupport()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().Contain("Full Controller Support");
    }

    [Fact]
    public void TestVR()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().NotContain("VR");
    }
    public void Dispose()
    {

    }
}
