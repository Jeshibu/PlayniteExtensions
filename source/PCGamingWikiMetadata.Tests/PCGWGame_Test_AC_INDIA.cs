using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace PCGamingWikiMetadata.Tests;

public class PCGWGame_Test_AC_INDIA : IDisposable
{
    private readonly PcgwGame testGame;
    private readonly LocalPCGWClient client;
    private readonly TestMetadataRequestOptions options;

    public PCGWGame_Test_AC_INDIA()
    {
        options = TestMetadataRequestOptions.BattleNet();
        client = new LocalPCGWClient(options);
        client.GetSettings().ImportTagMiddleware = true;
        client.GetSettings().TagPrefixMiddleware = "[Middleware]";
        testGame = new PcgwGame(client.GetSettings(), "Assassin's Creed Chronicles: India", -1);
        client.FetchGamePageContent(testGame);
    }

    [Fact]
    public void TestParseSeries()
    {
        var arr = testGame.Series.Select(i => i.ToString()).ToArray();
        arr.Should().Contain("Assassin's Creed Chronicles");
        arr.Should().Contain("Assassin's Creed");
    }

    [Fact]
    public void TestParseMiddleware()
    {
        var tags = testGame.Tags.Select(tag => tag.ToString()).ToArray();
        tags.Should().Contain("[Middleware] Physics: PhysX");
        tags.Should().Contain("[Middleware] Interface: Scaleform GFx");
        tags.Should().Contain("[Middleware] Cutscenes: Bink Video");
        tags.Where(t => t.Contains("[Middleware]")).Should().HaveCount(3);
    }

    public void Dispose()
    {

    }
}
