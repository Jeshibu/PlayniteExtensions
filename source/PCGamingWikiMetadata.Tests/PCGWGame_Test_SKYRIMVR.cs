using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace PCGamingWikiMetadata.Tests;

public class PCGWGame_Test_SKYRIMVR : IDisposable
{
    private readonly PCGWGame testGame;
    private readonly LocalPCGWClient client;
    private readonly TestMetadataRequestOptions options;


    public PCGWGame_Test_SKYRIMVR()
    {
        this.options = new TestMetadataRequestOptions();
        this.options.SetGameSourceSteam();
        this.client = new LocalPCGWClient(this.options);
        this.testGame = new PCGWGame(this.client.GetSettings(), "The Elder Scrolls V: Skyrim VR", -1);
        this.client.GetSettings().ImportFeatureVR = true;
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestVR()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().Contain("VR");
    }

    public void Dispose()
    {

    }
}
