using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace PCGamingWikiMetadata.Tests;

public class PCGWGame_Test_AC_INDIA : IDisposable
{
    private readonly PCGWGame testGame;
    private readonly LocalPCGWClient client;
    private readonly TestMetadataRequestOptions options;

    public PCGWGame_Test_AC_INDIA()
    {
        this.options = new TestMetadataRequestOptions();
        this.options.SetGameSourceBattleNet();
        this.client = new LocalPCGWClient(this.options);
        this.testGame = new PCGWGame(this.client.GetSettings(), "Assassin's Creed Chronicles: India", -1);
        this.client.FetchGamePageContent(this.testGame);
    }

    [Fact]
    public void TestParseSeries()
    {
        var arr = this.testGame.Series.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Assassin's Creed Chronicles");
        arr.Should().Contain("Assassin's Creed");
    }

    public void Dispose()
    {

    }
}
