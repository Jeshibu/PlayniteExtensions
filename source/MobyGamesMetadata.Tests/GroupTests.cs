using MobyGamesMetadata.Api;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Linq;
using Xunit;

namespace MobyGamesMetadata.Tests;

public class GroupTests
{
    [Fact]
    public void GetGamebryo()
    {
        const string firstPageUrl = "https://www.mobygames.com/group/4073/middleware-gamebryo-lightspeed-netimmerse/";
        var webViewFactory = new FakeWebViewFactory(new()
        {
            { firstPageUrl, "html/group-gamebryo-1.html" },
            { "https://www.mobygames.com/group/4073/middleware-gamebryo-lightspeed-netimmerse/sort:title/page:1/", "html/group-gamebryo-2.html" },
            { "https://www.mobygames.com/group/4073/middleware-gamebryo-lightspeed-netimmerse/sort:title/page:2/", "html/group-gamebryo-3.html" },
        });
        var scraper = new MobyGamesScraper(new PlatformUtility(), webViewFactory);
        var games = scraper.GetGamesFromGroup(firstPageUrl).ToList();

        Assert.Equal(120, games.Count);
        Assert.All(games, g => Assert.NotNull(g.Url));

        // Games that get shortened with a ... at the end of the title use their slugs as name instead
        var gits = games.Single(g => g.Url == "https://www.mobygames.com/game/94426/ghost-in-the-shell-stand-alone-complex-first-assault-online/");
        Assert.Equal("ghost-in-the-shell-stand-alone-complex-first-assault-online", gits.Names.Single());
    }
}