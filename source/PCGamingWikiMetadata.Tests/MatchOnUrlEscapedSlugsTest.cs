using PCGamingWikiBulkImport;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using Xunit;

namespace PCGamingWikiMetadata.Tests;

public class MatchOnUrlEscapedSlugsTest
{
    [Fact]
    public void NormalSlugMatchesEscapedSlug()
    {
        var libraryGame = new Game("Vivisector")
        {
            Links =
            [
                new Link("PCGamingWiki", "https://www.pcgamingwiki.com/wiki/Vivisector%20-%20Beast%20Within")
            ]
        };

        var matchHelper = new GameMatchingHelper(new PCGamingWikiIdUtility(), 1);
        matchHelper.Prepare([libraryGame], default);

        var slug = "Vivisector - Beast Within".TitleToSlug();
        var expectedId = DbId.PCGW(PCGamingWikiIdUtility.SlugToId(slug));

        Assert.True(matchHelper.TryGetGamesById(expectedId, out var games));

        Assert.NotEmpty(games);
    }
}
