using Xunit;
using PCGamingWikiMetadata;
using System;
using System.Linq;
using FluentAssertions;

public class PCGWGame_Test_BAT : IDisposable
{
    private PCGWGame testGame;
    private LocalPCGWClient client;
    private TestMetadataRequestOptions options;


    public PCGWGame_Test_BAT()
    {
        this.options = new TestMetadataRequestOptions();
        this.options.SetGameSourceEpic();
        this.client = new LocalPCGWClient(this.options);
        this.testGame = new PCGWGame(this.client.GetSettings(), "Batman: Arkham Knight", -1);
        this.client.GetSettings().ImportTagNoCloudSaves = false;
        this.client.GetSettings().ImportFeatureFramerate60 = true;
        this.client.GetSettings().ImportFeatureFramerate120 = true;
        this.client.GetSettings().ImportFeatureVR = true;
        this.client.GetSettings().ImportTagMonetization = true;
        this.client.GetSettings().ImportTagMicrotransactions = true;
        this.client.GetSettings().ImportLinkGOGDatabase = false;
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestParseWindowsReleaseDate()
    {
        var date = this.testGame.WindowsReleaseDate().ToString();
        date.Should().Match("6/23/2015");
    }

    [Fact]
    public void TestParseDevelopers()
    {
        var arr = this.testGame.Developers.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Rocksteady Studios", "Warner Bros. Games Montreal", "Iron Galaxy Studios");
    }

    [Fact]
    public void TestParsePublishers()
    {
        var arr = this.testGame.Publishers.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Warner Bros. Interactive Entertainment", "1C-SoftClub");
    }

    [Fact]
    public void TestParseSeries()
    {
        var arr = this.testGame.Series.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Batman: Arkham");
    }

    [Fact]
    public void TestParseGenres()
    {
        var arr = this.testGame.Genres.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Action", "Adventure", "Driving", "Metroidvania", "Stealth", "Open world");
    }

    [Fact]
    public void TestParsePerspectives()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Third-person");
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
        arr.Should().Contain("Automobile");
    }

    [Fact]
    public void TestParseMicrotransactions()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().NotContain("None");
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
        arr.Should().Contain("Contemporary", "North America");
    }
    [Fact]
    public void TestParseEngine()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Unreal Engine 3");
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
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().NotContain("No Cloud Saves");
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
        features.Should().NotContain("Local Multiplayer: Co-Op", "Local Multiplayer: Versus");
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
        features.Should().NotContain("120+ FPS");
    }

    [Fact]
    public void TestVR()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().NotContain("VR");
    }

    [Fact]
    public void TestLinks()
    {
        var links = this.testGame.Links.Select(i => i.Name).ToArray();
        links.Should().NotContain("WineHQ");
        links.Should().NotContain("GOG Database");
        links.Should().Contain("IGDB");
    }

    public void Dispose()
    {

    }
}
