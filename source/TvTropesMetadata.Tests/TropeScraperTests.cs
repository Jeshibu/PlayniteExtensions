using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;
using System.Linq;
using TvTropesMetadata.Scraping;
using Xunit;

namespace TvTropesMetadata.Tests;

public class TropeScraperTests
{
    private FakeWebDownloader downloader = new FakeWebDownloader(new Dictionary<string, string>
    {
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/TheAtoner", "html/TheAtoner.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/TheAtoner/VideoGames", "html/TheAtoner-VideoGames.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/StalkerWithACrush", "html/StalkerWithACrush.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VideoGames", "html/StalkerWithACrush-VideoGames.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VisualNovels", "html/StalkerWithACrush-VisualNovels.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/WallRun", "html/WallRun.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/WallJump", "html/WallJump.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/KillTheGod", "html/KillTheGod.html" },
    });

    [Fact]
    public void SubcategoryLinksParse()
    {
        var scraper = new TropeScraper(downloader);
        var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/StalkerWithACrush");

        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VideoGames", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VisualNovels", downloader.CalledUrls);

        Assert.NotNull(result);
        Assert.Equal("Stalker with a Crush", result.Title);
        Assert.NotEmpty(result.Items);

        ContainsGame(result, "Alice: Madness Returns", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/AliceMadnessReturns");
        ContainsGame(result, "Doki Doki Literature Club!", "https://tvtropes.org/pmwiki/pmwiki.php/VisualNovel/DokiDokiLiteratureClub");
    }

    [Fact]
    public void MixedSubcategoryAndFolderLinksParse()
    {
        var scraper = new TropeScraper(downloader);
        var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/TheAtoner");

        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/TheAtoner/VideoGames", downloader.CalledUrls);

        Assert.NotNull(result);
        Assert.Equal("The Atoner", result.Title);
        Assert.NotEmpty(result.Items);

        ContainsGame(result, "Double Homework", "https://tvtropes.org/pmwiki/pmwiki.php/VisualNovel/DoubleHomework");
        ContainsGame(result, "Bendy and the Ink Machine", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/BendyAndTheInkMachine");
    }

    [Fact]
    public void WallRunContainsTitanFall2()
    {
        var scraper = new TropeScraper(downloader);
        var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/WallRun");

        Assert.NotNull(result);
        Assert.Equal("Wall Run", result.Title);
        Assert.NotEmpty(result.Items);

        ContainsGame(result, "Titan Fall 2", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/TitanFall2");
    }

    [Fact]
    public void WallJumpContainsVideoGames()
    {
        var scraper = new TropeScraper(downloader);
        var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/WallJump");

        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/Main/WallJump", downloader.CalledUrls);

        Assert.NotNull(result);
        Assert.Equal("Wall Jump", result.Title);
        Assert.NotEmpty(result.Items);

        ContainsGame(result, "Shantae: Half-Genie Hero", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/ShantaeHalfGenieHero");
        Assert.DoesNotContain("Empowered", result.Items.SelectMany(i => i.Works).Select(w => w.Title));
    }

    [Fact]
    public void KillTheGodParsesRight()
    {
        var scraper = new TropeScraper(downloader);
        var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/KillTheGod");

        ContainsGame(result, "Nier Automata", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/NierAutomata");
        ContainsGame(result, "The Elder Scrolls V Skyrim", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/TheElderScrollsVSkyrim");
        ContainsGame(result, "Final Fantasy XIII-2", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/FinalFantasyXIII2");
        ContainsGame(result, "God of War", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/GodOfWar");
        ContainsGame(result, "Neverwinter Nights 2", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/NeverwinterNights2");
        ContainsGame(result, "Neverwinter Nights 2: Mask of the Betrayer", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/NeverwinterNights2");
        ContainsGame(result, "Pillars of Eternity", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/PillarsOfEternity");
    }

    private void ContainsGame(ParsedTropePage tropePage, string title, string url)
    {
        var work = tropePage.Items.SelectMany(i => i.Works).SingleOrDefault(w => w.Title == title);
        Assert.NotNull(work);
        Assert.Contains(url, work.Urls);
    }
}
