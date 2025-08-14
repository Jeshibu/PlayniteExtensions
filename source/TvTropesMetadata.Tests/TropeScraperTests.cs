using PlayniteExtensions.Metadata.Common;
using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;
using System.Linq;
using TvTropesMetadata.Scraping;
using TvTropesMetadata.SearchProviders;
using Xunit;

namespace TvTropesMetadata.Tests;

public class TropeScraperTests
{
    private readonly FakeWebDownloader downloader = new(new Dictionary<string, string>
    {
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/TheAtoner", "html/TheAtoner.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/TheAtoner/VideoGames", "html/TheAtoner-VideoGames.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/StalkerWithACrush", "html/StalkerWithACrush.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VideoGames", "html/StalkerWithACrush-VideoGames.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VisualNovels", "html/StalkerWithACrush-VisualNovels.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/WallRun", "html/WallRun.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/WallJump", "html/WallJump.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/KillTheGod", "html/KillTheGod.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/PlayableEpilogue", "html/PlayableEpilogue.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/Main/MultipleEndings", "html/MultipleEndings.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/VideoGames", "html/MultipleEndings-VideoGames.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/ActionGames", "html/MultipleEndings-ActionGames.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/AdventureGames", "html/MultipleEndings-AdventureGames.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/NotForBroadcast", "html/MultipleEndings-NotForBroadcast.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/TheStanleyParable", "html/MultipleEndings-TheStanleyParable.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/RolePlayingGames", "html/MultipleEndings-RolePlayingGames.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/BaldursGateIII", "html/MultipleEndings-BaldursGateIII.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/LonelyWolfTreat", "html/MultipleEndings-LonelyWolfTreat.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/ShinMegamiTensei", "html/MultipleEndings-ShinMegamiTensei.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/Undertale", "html/MultipleEndings-Undertale.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/TheHundredLineLastDefenseAcademy", "html/MultipleEndings-TheHundredLineLastDefenseAcademy.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/SurvivalHorrorGames", "html/MultipleEndings-SurvivalHorrorGames.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/VisualNovels", "html/MultipleEndings-VisualNovels.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/ClassOf09", "html/MultipleEndings-ClassOf09.html" },
        { "https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/NeedyStreamerOverload", "html/MultipleEndings-NeedyStreamerOverload.html" },
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

    [Fact]
    public void VideogameOnlyTropeParsesRight()
    {
        var scraper = new TropeScraper(downloader);
        var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/PlayableEpilogue");

        Assert.Equal("Playable Epilogue", result.Title);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public void VideogameSubcategoriesMixedWithGamesParseRight()
    {
        var scraper = new TropeScraper(downloader);
        var sp = new TropeSearchProvider(scraper, new TvTropesMetadataSettings { OnlyFirstGamePerTropeListItem = false });
        //var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/MultipleEndings");
        var result = sp.GetDetails("https://tvtropes.org/pmwiki/pmwiki.php/Main/MultipleEndings").ToList();

        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/Main/MultipleEndings", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/VideoGames", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/ActionGames", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/AdventureGames", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/NotForBroadcast", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/TheStanleyParable", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/RolePlayingGames", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/BaldursGateIII", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/LonelyWolfTreat", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/ShinMegamiTensei", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/Undertale", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/TheHundredLineLastDefenseAcademy", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/SurvivalHorrorGames", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/VisualNovels", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/ClassOf09", downloader.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/NeedyStreamerOverload", downloader.CalledUrls);
        Assert.DoesNotContain("https://tvtropes.org/pmwiki/pmwiki.php/Main/AlgorithmicStoryBranching", downloader.CalledUrls);

        //from the breadcrumb headers of the game's subcategory pages - these don't appear elsewhere
        ContainsGame(result, "The Stanley Parable", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/TheStanleyParable");
        ContainsGame(result, "The Hundred Line -Last Defense Academy-", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/TheHundredLineLastDefenseAcademy");

        ContainsGame(result, "DATE TREAT", null);
    }

    private void ContainsGame(ParsedTropePage tropePage, string title, string url)
    {
        var work = tropePage.Items.SelectMany(i => i.Works).SingleOrDefault(w => w.Title == title);
        Assert.NotNull(work);

        if (url == null)
            Assert.Empty(work.Urls);
        else
            Assert.Contains(url, work.Urls);
    }

    private void ContainsGame(IEnumerable<GameDetails> games, string title, string url)
    {
        var titleMatch = games.SingleOrDefault(g => g.Names.Contains(title));
        Assert.NotNull(titleMatch);
        Assert.Equal(url, titleMatch.Url);
    }
}
