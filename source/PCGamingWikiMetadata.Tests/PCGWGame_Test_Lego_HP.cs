using Xunit;
using PCGamingWikiMetadata;
using System;
using System.Linq;
using FluentAssertions;

public class PCGWGame_Test_Lego_HP : IDisposable
{
    private PCGWGame testGame;
    private LocalPCGWClient client;
    private TestMetadataRequestOptions options;


    public PCGWGame_Test_Lego_HP()
    {
        this.options = new TestMetadataRequestOptions();
        this.options.SetGameSourceSteam();
        this.client = new LocalPCGWClient(this.options);
        this.testGame = new PCGWGame(this.client.GetSettings(), "Lego Harry Potter: Years 1-4", -1);
        this.client.GetSettings().ImportTagNoCloudSaves = true;
        this.client.GetSettings().ImportFeatureVR = true;
        this.client.GetSettings().ImportFeatureFramerate60 = true;
        this.client.GetSettings().ImportFeatureFramerate120 = true;
        this.client.GetSettings().ImportMultiplayerTypes = true;
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestParseWindowsReleaseDate()
    {
        var date = this.testGame.WindowsReleaseDate().ToString();
        date.Should().Match("6/25/2010");
    }

    [Fact]
    public void TestParseDevelopers()
    {
        var arr = this.testGame.Developers.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Traveller's Tales", "Open Planet Software", "Feral Interactive");
    }

    [Fact]
    public void TestParsePublishers()
    {
        var arr = this.testGame.Publishers.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Warner Bros. Interactive Entertainment", "Feral Interactive");
    }

    [Fact]
    public void TestParseSeries()
    {
        var arr = this.testGame.Series.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Lego Harry Potter");
    }

    [Fact]
    public void TestParseGenres()
    {
        var arr = this.testGame.Genres.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Action", "Adventure");
    }

    [Fact]
    public void TestParsePerspectives()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Cinematic camera");
    }

    [Fact]
    public void TestParseControls()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Direct control");
    }

    [Fact]
    public void TestParseVehicles()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Automobile", "Flight");
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
        arr.Should().Contain("Contemporary", "Fantasy");
    }
    [Fact]
    public void TestParseEngine()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Nu2");
    }

    [Fact]
    public void TestParseModes()
    {
        var arr = this.testGame.Features.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Singleplayer", "Multiplayer");
    }

    [Fact]
    public void TestCloudSaves()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("No Cloud Saves");
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
        features.Should().NotContain("Online Multiplayer: Co-Op", "Online Multiplayer: Versus");
        features.Should().NotContain("LAN Multiplayer: Co-Op", "LAN Multiplayer: Versus");
        features.Should().Contain("Local Multiplayer: Co-op", "Local Multiplayer: 2");
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
        features.Should().Contain("60 FPS");
        features.Should().Contain("120+ FPS");
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
