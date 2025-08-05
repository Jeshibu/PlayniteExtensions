using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace PCGamingWikiMetadata.Tests;

public class PCGWGame_Test_WH_40K_SPACE_MARINE : IDisposable
{
    private readonly PCGWGame testGame;
    private readonly LocalPCGWClient client;
    private readonly TestMetadataRequestOptions options;

    public PCGWGame_Test_WH_40K_SPACE_MARINE()
    {
        this.options = new TestMetadataRequestOptions();
        this.options.SetGameSourceBattleNet();
        this.client = new LocalPCGWClient(this.options);
        this.testGame = new PCGWGame(this.client.GetSettings(), "Warhammer 40,000: Space Marine", -1);
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestParseSeries()
    {
        var arr = this.testGame.Series.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Warhammer 40,000: Space Marine");
    }

    public void Dispose()
    {

    }
}
