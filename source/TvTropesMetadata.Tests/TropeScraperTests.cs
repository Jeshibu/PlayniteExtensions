using PlayniteExtensions.Metadata.Common;
using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TvTropesMetadata.Scraping;
using TvTropesMetadata.SearchProviders;
using Xunit;

namespace TvTropesMetadata.Tests;

public class TropeScraperTests
{
    private class FakeTropeWebViewFactory(Dictionary<string, string> sourceFilesByUrl) : FakeWebViewFactory(sourceFilesByUrl)
    {
        protected override FakeWebView WebView => field ??= new FakeTropeWebView(sourceFilesByUrl);
    }

    private class FakeTropeWebView(Dictionary<string, string> sourceFilesByUrl) : FakeWebView(sourceFilesByUrl)
    {
        private static readonly Regex UrlRegex = new(@"^https://tvtropes\.org/pmwiki/pmwiki\.php/(\w+)/(\w+)$", RegexOptions.Compiled);

        private static string GetFileContents(string url)
        {
            var match = UrlRegex.Match(url);
            if (!match.Success)
                return null;

            string gr1 = match.Groups[1].Value;
            string gr2 = match.Groups[2].Value;
            string filePath = gr1 == "Main" ? $"html/{gr2}.html" : $"html/{gr1}-{gr2}.html";
            if (!File.Exists(filePath))
                return null;

            return File.ReadAllText(filePath);
        }

        public override string GetPageSource() => GetFileContents(Url) ?? base.GetPageSource();
    }

    private readonly FakeTropeWebViewFactory webViewFactory = new(new()
    {
        { BaseScraper.GetGoogleSearchUrl("endings"), "html/google-endings.html" },
    });

    [Fact]
    public void SubcategoryLinksParse()
    {
        var scraper = new TropeScraper(webViewFactory);
        var sp = new TropeSearchProvider(scraper, new TvTropesMetadataSettings { OnlyFirstGamePerTropeListItem = false });
        var result = sp.GetDetails("https://tvtropes.org/pmwiki/pmwiki.php/Main/StalkerWithACrush").ToList();

        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/StalkerWithACrush/VideoGames", webViewFactory.CalledUrls);

        Assert.NotEmpty(result);

        ContainsGame(result, "Alice: Madness Returns", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/AliceMadnessReturns");
        ContainsGame(result, "Doki Doki Literature Club!", "https://tvtropes.org/pmwiki/pmwiki.php/VisualNovel/DokiDokiLiteratureClub");
    }

    [Fact]
    public void MixedSubcategoryAndFolderLinksParse()
    {
        var scraper = new TropeScraper(webViewFactory);
        var sp = new TropeSearchProvider(scraper, new TvTropesMetadataSettings { OnlyFirstGamePerTropeListItem = false });
        var result = sp.GetDetails("https://tvtropes.org/pmwiki/pmwiki.php/Main/TheAtoner").ToList();

        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/TheAtoner/VideoGames", webViewFactory.CalledUrls);

        Assert.NotEmpty(result);

        ContainsGame(result, "Double Homework", "https://tvtropes.org/pmwiki/pmwiki.php/VisualNovel/DoubleHomework");
        ContainsGame(result, "Bendy and the Ink Machine", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/BendyAndTheInkMachine");
    }

    [Fact]
    public void WallRunContainsTitanFall2()
    {
        var scraper = new TropeScraper(webViewFactory);
        var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/WallRun");

        Assert.NotNull(result);
        Assert.Equal("Wall Run", result.Title);
        Assert.NotEmpty(result.Items);

        ContainsGame(result, "Titanfall 2", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/TitanFall2");
    }

    [Fact]
    public void WallJumpContainsVideoGames()
    {
        var scraper = new TropeScraper(webViewFactory);
        var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/WallJump");

        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/Main/WallJump", webViewFactory.CalledUrls);

        Assert.NotNull(result);
        Assert.Equal("Wall Jump", result.Title);
        Assert.NotEmpty(result.Items);

        ContainsGame(result, "Shantae: Half-Genie Hero", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/ShantaeHalfGenieHero");
        Assert.DoesNotContain("Empowered", result.Items.SelectMany(i => i.Works).Select(w => w.Title));
    }

    [Fact]
    public void KillTheGodParsesRight()
    {
        var scraper = new TropeScraper(webViewFactory);
        var sp = new TropeSearchProvider(scraper, new TvTropesMetadataSettings { OnlyFirstGamePerTropeListItem = false });
        var result = sp.GetDetails("https://tvtropes.org/pmwiki/pmwiki.php/Main/KillTheGod").ToList();

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
        var scraper = new TropeScraper(webViewFactory);
        var result = scraper.GetGamesForTrope("https://tvtropes.org/pmwiki/pmwiki.php/Main/PlayableEpilogue");

        Assert.Equal("Playable Epilogue", result.Title);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public void VideogameSubcategoriesMixedWithGamesParseRight()
    {
        var scraper = new TropeScraper(webViewFactory);
        var sp = new TropeSearchProvider(scraper, new TvTropesMetadataSettings { OnlyFirstGamePerTropeListItem = false });
        var result = sp.GetDetails("https://tvtropes.org/pmwiki/pmwiki.php/Main/MultipleEndings").ToList();

        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/Main/MultipleEndings", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/VideoGames", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/ActionGames", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/AdventureGames", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/NotForBroadcast", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/TheStanleyParable", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/RolePlayingGames", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/BaldursGateIII", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/LonelyWolfTreat", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/ShinMegamiTensei", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/Undertale", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/TheHundredLineLastDefenseAcademy", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/SurvivalHorrorGames", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/VisualNovels", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/ClassOf09", webViewFactory.CalledUrls);
        Assert.Contains("https://tvtropes.org/pmwiki/pmwiki.php/MultipleEndings/NeedyStreamerOverload", webViewFactory.CalledUrls);
        Assert.DoesNotContain("https://tvtropes.org/pmwiki/pmwiki.php/Main/AlgorithmicStoryBranching", webViewFactory.CalledUrls);

        //from the breadcrumb headers of the game's subcategory pages - these don't appear elsewhere
        ContainsGame(result, "The Hundred Line -Last Defense Academy-", null);

        ContainsGame(result, "DATE TREAT", null);
    }

    [Fact]
    public void ActionCategoryParses()
    {
        var scraper = new TropeScraper(webViewFactory);
        var sp = new TropeSearchProvider(scraper, new TvTropesMetadataSettings { OnlyFirstGamePerTropeListItem = false });
        var result = sp.GetDetails("https://tvtropes.org/pmwiki/pmwiki.php/Main/JigglePhysics").ToList();

        ContainsGame(result, "Asura's Wrath", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/AsurasWrath");
    }

    [Fact]
    public void GamesReturnWithMultipleNames()
    {
        var scraper = new TropeScraper(webViewFactory);
        var sp = new TropeSearchProvider(scraper, new TvTropesMetadataSettings { OnlyFirstGamePerTropeListItem = false });
        var result = sp.GetDetails("https://tvtropes.org/pmwiki/pmwiki.php/Main/DoubleJump").ToList();

        ContainsGame(result, ["Ultimate Spider-Man", "Ultimate Spider Man 2005"], "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/UltimateSpiderMan2005");
    }

    [Fact]
    public void PartialNameLinksParseAsBoth()
    {
        var scraper = new TropeScraper(webViewFactory);
        var sp = new TropeSearchProvider(scraper, new TvTropesMetadataSettings { OnlyFirstGamePerTropeListItem = false });
        var result = sp.GetDetails("https://tvtropes.org/pmwiki/pmwiki.php/Main/JigglePhysics").ToList();

        ContainsGame(result, ["Dead or Alive 5 Plus", "Dead Or Alive 5"], "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/DeadOrAlive5");
        ContainsGame(result, ["Dead or Alive 5 Ultimate", "Dead Or Alive 5"], "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/DeadOrAlive5");
        ContainsGame(result, ["Dead or Alive 5: Last Round"], null);
    }

    [Fact]
    public void SearchProducesResults()
    {
        var scraper = new TropeScraper(webViewFactory);
        var sp = new TropeSearchProvider(scraper, new());

        var searchResults = sp.Search("endings").ToList();

        Assert.Equal(7, searchResults.Count);
        Assert.Equal("Multiple Endings", searchResults[0].Name);
        Assert.Equal("https://tvtropes.org/pmwiki/pmwiki.php/Main/MultipleEndings", searchResults[0].Url);
        Assert.Equal("Multiple Endings are the most commonly seen form of Story Branching in video games, used primarily to increase their Replay Value.", searchResults[0].Description);
        Assert.Equal("Tropes", searchResults[0].Breadcrumbs.Single());
    }

    [Fact]
    public void ScoringPoints()
    {
        var result = GetTropeDetails("ScoringPoints");

        ContainsGame(result, "Civilization", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/Civilization");
        ContainsGame(result, "MadWorld", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/MadWorld");
        ContainsGame(result, "Wolfenstein 3-D", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/Wolfenstein3D");
        ContainsGame(result, "World of Warcraft", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/WorldOfWarcraft");
        ContainsGame(result, "Wii Play", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/WiiPlay");
        ContainsGame(result, "Overcooked!", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/Overcooked");
        ContainsGame(result, "Time Crisis", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/TimeCrisis");
        ContainsGame(result, "NetHack", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/NetHack");
        ContainsGame(result, "Geometry Wars", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/GeometryWars");
        ContainsGame(result, "Grid Wars", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/GridWars");
        ContainsGame(result, "The Club", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/TheClub");
        ContainsGame(result, "Defense Grid: The Awakening", "https://tvtropes.org/pmwiki/pmwiki.php/Videogame/DefenseGridTheAwakening");
        ContainsGame(result, "Total Overdose", "https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/TotalOverdose");
    }

    [Theory]
    [InlineData("WhiteElephantUnit", new[] { "White Elephant", "Everdell", "Chess" })]
    [InlineData("AntiHoarding", new[] { "Everdell", "Illuminati: New World Order", "Dungeons & Dragons" })]
    [InlineData("ActionInitiative", new[] { "Magic: The Gathering", "BattleTech" })]
    [InlineData("ExtraTurn", new[] { "Wheel of Fortune", "Cardfight!! Vanguard", "Monopoly", "Star Trek", "Video Village" })]
    public void TropeResultDoesNotContainNonVideogameWorks(string urlTitle, string[] namesThatShouldNotBeHere)
    {
        var result = GetTropeDetails(urlTitle);

        var allGameNames = result.SelectMany(g => g.Names).ToList();

        Assert.NotEmpty(allGameNames);

        foreach (string name in namesThatShouldNotBeHere)
            Assert.DoesNotContain(name, allGameNames);
    }

    private List<GameDetails> GetTropeDetails(string urlTitle, bool onlyFirstGame = false)
    {
        var scraper = new TropeScraper(webViewFactory);
        var sp = new TropeSearchProvider(scraper, new TvTropesMetadataSettings { OnlyFirstGamePerTropeListItem = onlyFirstGame });
        return sp.GetDetails("https://tvtropes.org/pmwiki/pmwiki.php/Main/" + urlTitle).ToList();
    }

    private static TvTropesWork ContainsGame(ParsedTropePage tropePage, string title, string url)
    {
        var titleMatches = tropePage.Items.SelectMany(i => i.Works).Where(w => w.Title == title).ToList();
        Assert.Single(titleMatches);

        var work = titleMatches.Single();
        Assert.NotNull(work);

        if (url == null)
            Assert.Empty(work.Urls);
        else
            Assert.Contains(url, work.Urls);

        return work;
    }

    private static GameDetails ContainsGame(IEnumerable<GameDetails> games, string title, string url)
    {
        var titleMatch = games.SingleOrDefault(g => g.Names.Contains(title));
        Assert.NotNull(titleMatch);
        Assert.Equal(url, titleMatch.Url);
        return titleMatch;
    }

    private static GameDetails ContainsGame(IEnumerable<GameDetails> games, string[] titles, string url)
    {
        var game = ContainsGame(games, titles[0], url);
        AssertHelper.CollectionsHaveSameItems(titles, game.Names);
        return game;
    }
}
