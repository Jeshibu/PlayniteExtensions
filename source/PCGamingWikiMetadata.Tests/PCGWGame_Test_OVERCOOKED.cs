using Xunit;
using PCGamingWikiMetadata;
using System;
using System.Linq;
using FluentAssertions;

public class PCGWGame_Test_OVERCOOKED : IDisposable
{
    private PCGWGame testGame;
    private LocalPCGWClient client;
    private TestMetadataRequestOptions options;

    public PCGWGame_Test_OVERCOOKED()
    {
        this.options = new TestMetadataRequestOptions();
        this.options.SetGameSourceOrigin();
        this.client = new LocalPCGWClient(this.options);
        this.testGame = new PCGWGame(this.client.GetSettings(), "Overcooked!", -1);
        this.client.GetSettings().ImportFeatureVR = true;
        this.client.GetSettings().ImportMultiplayerTypes = true;
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestParseWindowsReleaseDate()
    {
        var date = this.testGame.WindowsReleaseDate().ToString();
        date.Should().Match("8/3/2016");
    }

    [Fact]
    public void TestParseDevelopers()
    {
        var arr = this.testGame.Developers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Ghost Town Games");
    }

    [Fact]
    public void TestParsePublishers()
    {
        var arr = this.testGame.Publishers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Team17");
    }

    [Fact]
    public void TestParseGenres()
    {
        var arr = this.testGame.Genres.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Action", "Party game", "Simulation", "Time management");
    }

    [Fact]
    public void TestParsePerspectives()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Bird's-eye view");
    }

    [Fact]
    public void TestParseControls()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Direct control");
    }

    [Fact]
    public void TestParsePacing()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Real-time");
    }

    [Fact]
    public void TestParseThemes()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Fantasy");
    }
    [Fact]
    public void TestParseEngine()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Unity 5");
    }

    [Fact]
    public void TestParseModes()
    {
        var arr = this.testGame.Features.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Singleplayer");
    }

    [Fact]
    public void TestCloudSaves()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().Contain("Cloud Saves");

        var tags = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        tags.Should().NotContain("No Cloud Saves");
    }

    [Fact]
    public void TestControllerSupport()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().Contain("Full Controller Support", "Controller Support");
    }

    [Fact]
    public void TestMultiplayer()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().NotContain("Online Multiplayer", "Online Multiplayer: Co-op", "Online Multiplayer: Versus");
        features.Should().NotContain("LAN Multiplayer", "LAN Multiplayer: Co-op", "LAN Multiplayer: Versus");
        features.Should().Contain("Local Multiplayer: 2-4", "Local Multiplayer: Co-op", "Local Multiplayer: Versus");
    }

    [Fact]
    public void TestHDR()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().NotContain("HDR");
    }

    [Fact]
    public void TestRayTracing()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().NotContain("Ray Tracing");
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
