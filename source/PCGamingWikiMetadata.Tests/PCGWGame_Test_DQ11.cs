using Xunit;
using PCGamingWikiMetadata;
using System;
using System.Linq;
using FluentAssertions;

public class PCGWGame_Test_DQ11 : IDisposable
{
    private PCGWGame testGame;
    private LocalPCGWClient client;
    private TestMetadataRequestOptions options;

    public PCGWGame_Test_DQ11()
    {
        this.options = new TestMetadataRequestOptions();
        this.options.SetGameSourceXbox();
        this.client = new LocalPCGWClient(this.options);
        this.testGame = new PCGWGame(this.client.GetSettings(), "Dragon Quest XI S - Definitive Edition", -1);

        this.client.GetSettings().ImportTagEngine = false;
        this.client.GetSettings().ImportTagArtStyle = false;
        this.client.GetSettings().ImportTagMonetization = false;
        this.client.GetSettings().ImportTagMicrotransactions = false;
        this.client.GetSettings().ImportTagPacing = false;
        this.client.GetSettings().ImportTagPerspectives = false;
        this.client.GetSettings().ImportTagControls = false;
        this.client.GetSettings().ImportTagVehicles = false;
        this.client.GetSettings().ImportTagThemes = false;
        this.client.GetSettings().ImportTagArtStyle = false;
        this.client.GetSettings().ImportXboxPlayAnywhere = false;
        this.client.GetSettings().ImportFeatureHDR = true;
        this.client.GetSettings().ImportFeatureRayTracing = true;
        this.client.GetSettings().ImportFeatureVR = true;
        this.client.GetSettings().ImportLinkProtonDB = true;
        this.client.GetSettings().ImportLinkIGDB = false;

        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestParseWindowsReleaseDate()
    {
        var date = this.testGame.WindowsReleaseDate().ToString();
        date.Should().Match("12/4/2020");
    }

    [Fact]
    public void TestParseDevelopers()
    {
        var arr = this.testGame.Developers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Square Enix", "ArtePiazza", "Orca");
    }

    [Fact]
    public void TestParsePublishers()
    {
        var arr = this.testGame.Publishers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Square Enix");
    }

    [Fact]
    public void TestParseSeries()
    {
        var arr = this.testGame.Series.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Dragon Quest");
    }

    [Fact]
    public void TestParseGenres()
    {
        var arr = this.testGame.Genres.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("RPG", "JRPG");
    }

    [Fact]
    public void TestParsePerspectives()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().NotContain("Third-person", "Top-down view");
    }

    [Fact]
    public void TestParseControls()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().NotContain("Menu-based", "Direct control");
    }

    [Fact]
    public void TestParseVehicles()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().NotContain("Flight", "Naval/watercraft", "Track racing");
    }

    [Fact]
    public void TestParseArtStyles()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().NotContain("Anime", "Pixel art");
    }

    [Fact]
    public void TestParsePacing()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().NotContain("Turn-based");
    }
    [Fact]
    public void TestParseEngine()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        // this.client.GetSettings().ImportTagEngine.Should().BeFalse();
        arr.Should().NotContain("Unreal Engine 4");
    }

    [Fact]
    public void TestParseModes()
    {
        var arr = this.testGame.Features.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Singleplayer");
    }

    [Fact]
    public void TestParseXboxPlayAnywhere()
    {
        var arr = this.testGame.Features.Select(i => i.ToString()).ToArray();
        arr.Should().NotContain("Xbox Play Anywhere");
    }

    [Fact]
    public void TestControllerSupport()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().Contain("Full Controller Support");
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

    [Fact]
    public void TestLinks()
    {
        var links = this.testGame.Links.Select(i => i.Name).ToArray();
        links.Should().Contain("ProtonDB");
        links.Should().NotContain("IGDB");
    }

    public void Dispose()
    {

    }
}
