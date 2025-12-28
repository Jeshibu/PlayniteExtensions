using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace PCGamingWikiMetadata.Tests;

public class PCGWGame_Test_CATLADY : IDisposable
{
    private readonly PcgwGame testGame;
    private readonly LocalPCGWClient client;
    private readonly TestMetadataRequestOptions options;


    public PCGWGame_Test_CATLADY()
    {
        this.options = TestMetadataRequestOptions.Steam();
        this.client = new LocalPCGWClient(this.options);
        this.testGame = new PcgwGame(this.client.GetSettings(), "Cat Lady - The Card Game", -1);
        // this.client.GetSettings().ImportTagNoCloudSaves = false;
        // this.client.GetSettings().ImportFeatureFramerate60 = true;
        // this.client.GetSettings().ImportFeatureFramerate120 = true;
        // this.client.GetSettings().ImportFeatureVR = true;
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestTouchscreenSupport()
    {
        var features = this.testGame.Features.Select(i => i.ToString()).ToArray();
        features.Should().Contain("Touchscreen optimised");
    }

    public void Dispose()
    {

    }
}
