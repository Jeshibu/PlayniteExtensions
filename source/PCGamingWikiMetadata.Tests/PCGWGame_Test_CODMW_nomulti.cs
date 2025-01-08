using Xunit;
using PCGamingWikiMetadata;
using System;
using System.Linq;
using FluentAssertions;

public class PCGWGame_Test_CODMW_nomulti : IDisposable
{
    private PCGWGame testGame;
    private LocalPCGWClient client;
    private TestMetadataRequestOptions options;

    public PCGWGame_Test_CODMW_nomulti()
    {
        this.options = new TestMetadataRequestOptions();
        this.options.SetGameSourceBattleNet();
        this.client = new LocalPCGWClient(this.options);
        this.testGame = new PCGWGame(this.client.GetSettings(), "Call of Duty: Modern Warfare", -1);
        this.client.GetSettings().ImportMultiplayerTypes = false;
        this.client.GetSettings().ImportFeatureHDR = false;
        this.client.GetSettings().ImportFeatureRayTracing = false;
        this.client.GetSettings().ImportFeatureFramerate60 = false;
        this.client.GetSettings().ImportFeatureFramerate120 = true;
        this.client.GetSettings().ImportFeatureUltrawide = false;
        this.client.GetSettings().ImportFeatureVR = true;
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestParseWindowsReleaseDate()
    {
        var date = this.testGame.WindowsReleaseDate().ToString();
        date.Should().Match("10/25/2019");
    }

    [Fact]
    public void TestParseDevelopers()
    {
        var arr = this.testGame.Developers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Infinity Ward", "High Moon Studios", "Sledgehammer Games", "Raven Software", "Beenox");
    }

    [Fact]
    public void TestParsePublishers()
    {
        var arr = this.testGame.Publishers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Activision");
    }

    [Fact]
    public void TestParseSeries()
    {
        var arr = this.testGame.Series.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Call of Duty: Modern Warfare");
    }

    [Fact]
    public void TestParseGenres()
    {
        var arr = this.testGame.Genres.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Action", "Shooter", "Battle royale");
    }

    [Fact]
    public void TestParsePerspectives()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("First-person");
    }

    [Fact]
    public void TestParseControls()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Direct control");
    }

    [Fact]
    public void TestParseArtStyles()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Realistic");
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
        arr.Should().Contain("Middle East");
    }

    [Fact]
    public void TestParseModes()
    {
        var arr = this.testGame.Features.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Singleplayer", "Multiplayer");
    }

    [Fact]
    public void TestControllerSupport()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().Contain("Full Controller Support");
    }

    [Fact]
    public void TestMultiplayer()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().NotContain("Online Multiplayer: 8+", "Online Multiplayer: Co-op", "Online Multiplayer: Versus");
        features.Should().NotContain("LAN Multiplayer", "LAN Multiplayer: Co-op", "LAN Multiplayer: Versus");
        features.Should().NotContain("Local Multiplayer", "Local Multiplayer: Co-op", "Local Multiplayer: Versus");
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
    public void TestFPS()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().NotContain("60 FPS");
        features.Should().Contain("120+ FPS");
    }

    [Fact]
    public void TestUltrawide()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().NotContain("Ultra-widescreen");
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
