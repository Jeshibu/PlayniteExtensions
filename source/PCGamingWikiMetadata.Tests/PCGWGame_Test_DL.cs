using Xunit;
using PCGamingWikiMetadata;
using System;
using System.Linq;
using FluentAssertions;

public class PCGWGame_Test_DL : IDisposable
{
    private PCGWGame testGame;
    private LocalPCGWClient client;

    public PCGWGame_Test_DL()
    {
        this.client = new LocalPCGWClient();
        this.testGame = new PCGWGame(this.client.GetSettings(), "Deathloop", -1);
        this.client.GetSettings().ImportMultiplayerTypes = true;
        this.client.GetSettings().ImportFeatureVR = true;
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestParseWindowsReleaseDate()
    {
        var date = this.testGame.WindowsReleaseDate().ToString();
        date.Should().Match("9/14/2021");
    }

    [Fact]
    public void TestParseDevelopers()
    {
        var arr = this.testGame.Developers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Arkane Studios");
    }

    [Fact]
    public void TestParsePublishers()
    {
        var arr = this.testGame.Publishers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Bethesda Softworks");
    }

    [Fact]
    public void TestParseSeries()
    {
        var arr = this.testGame.Series.Select(i => i.ToString()).ToArray();
        arr.Should().BeEmpty();
    }

    [Fact]
    public void TestParseGenres()
    {
        var arr = this.testGame.Genres.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Immersive sim", "Shooter", "Action");
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
        arr.Should().Contain("Stylized");
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
        arr.Should().Contain("Supernatural");
    }
    [Fact]
    public void TestParseEngine()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Void Engine");
    }

    [Fact]
    public void TestParseModes()
    {
        var arr = this.testGame.Features.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Singleplayer", "Multiplayer");
    }

    [Fact]
    public void TestMultiplayer()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().Contain("Online Multiplayer: Versus");
        features.Should().NotContain("Online Multiplayer: Co-Op", "LAN Multiplayer: Co-Op", "LAN Multiplayer: Versus");
        features.Should().NotContain("Local Multiplayer: Co-Op", "Local Multiplayer: Versus");
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
