using Xunit;
using PCGamingWikiMetadata;
using System;
using System.Linq;
using FluentAssertions;

public class PCGWGame_Test_CITIES : IDisposable
{
    private PCGWGame testGame;
    private LocalPCGWClient client;
    private TestMetadataRequestOptions options;

    public PCGWGame_Test_CITIES()
    {
        this.options = new TestMetadataRequestOptions();
        this.options.SetGameSourceSteam();
        this.client = new LocalPCGWClient(this.options);
        this.testGame = new PCGWGame(this.client.GetSettings(), "Cities: Skylines", -1);
        this.client.GetSettings().ImportLinkOfficialSite = false;
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestParseWindowsReleaseDate()
    {
        var date = this.testGame.WindowsReleaseDate().ToString();
        date.Should().Match("3/10/2015");
    }

    [Fact]
    public void TestParseDevelopers()
    {
        var arr = this.testGame.Developers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Colossal Order", "Tantalus Media");
    }

    [Fact]
    public void TestParsePublishers()
    {
        var arr = this.testGame.Publishers.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Paradox Interactive");
    }

    [Fact]
    public void TestParseGenres()
    {
        var arr = this.testGame.Genres.Select(i => i.ToString()).ToArray();
        arr.Should().BeEquivalentTo("Building", "Simulation", "Strategy", "Business");
    }

    [Fact]
    public void TestParsePerspectives()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Bird's-eye view");
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
    public void TestParseControls()
    {
        var arr = this.testGame.Tags.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Point and select");
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
        arr.Should().Contain("Contemporary");
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
        features.Should().NotContain("Full Controller Support");
    }

    [Fact]
    public void TestLinks()
    {
        var features = this.testGame.Links.Select(i => i.Name).ToArray();
        features.Should().NotContain("Official site");
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
