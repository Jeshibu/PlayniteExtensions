using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;
using Xunit;

namespace Barnite.Tests.MobyGames;

public class MobyGamesScraperTests
{
    [Fact]
    public void ScrapingCallOfCthulhuReturnsCorrectMetadata()
    {
        var platformUtility = new PlatformUtility(new Dictionary<string, string[]> { { "Xbox", ["xbox"] }, { "Windows", ["pc_windows"] } });
        var gameUrl = "https://www.mobygames.com/game/20705/call-of-cthulhu-dark-corners-of-the-earth/";

        var webclient = new FakeWebDownloader(new Dictionary<string, string> {
                { "https://www.mobygames.com/search/?q=093155118706&type=game", "./MobyGames/cocsearch.html" },
                { gameUrl, "./MobyGames/coc.html" }
            });
        var scraper = new MobyGamesScraper();
        scraper.Initialize(new PlatformUtility("Xbox", "xbox"), webclient);

        var data = scraper.GetMetadataFromBarcode("093155118706");

        Assert.NotNull(data);
        Assert.Equal("Call of Cthulhu: Dark Corners of the Earth", data.Name);
        Assert.Equal(2, data.Platforms.Count);
        Assert.Single(data.Platforms, new MetadataSpecProperty("xbox"));
        Assert.Single(data.Platforms, new MetadataSpecProperty("pc_windows"));
        ContainsProperty(data.Publishers, "2K Games");
        ContainsProperty(data.Publishers, "Bethesda Softworks");
        Assert.Single(data.Developers, new MetadataNameProperty("Headfirst Productions"));
        Assert.Equal(new ReleaseDate(2005, 10, 24), data.ReleaseDate);
        ContainsProperty(data.Genres, "Action");
        ContainsProperty(data.Genres, "1st-person");
        ContainsProperty(data.Genres, "Stealth");
        ContainsProperty(data.Genres, "Survival horror");
        ContainsProperty(data.Genres, "Detective / mystery");
        ContainsProperty(data.Genres, "Horror");
        ContainsProperty(data.Tags, "Interwar");
        ContainsProperty(data.Tags, "Licensed");
        ContainsProperty(data.Tags, "Regional differences");
        ContainsProperty(data.Tags, "Console Generation Exclusives: Xbox");
        ContainsProperty(data.Tags, "HUDless games");
        ContainsProperty(data.Tags, "Inspiration: Author - H. P. Lovecraft");
        ContainsProperty(data.Tags, "Inspiration: Literature");
        ContainsProperty(data.Tags, "Setting: 1920s");
        ContainsProperty(data.Tags, "Setting: City - Boston");
        ContainsProperty(data.Tags, "Ubisoft eXclusive releases");
        Assert.Equal(@"<p>Detective Jack Walters arrived in Innsmouth to solve a case of a missing person. But soon he finds himself confronted with terrible mysteries older than humanity, and with ghosts of the mysterious events that led to his incarceration in a mental hospital years ago. </p>
<p><em>Call of Cthulhu: Dark Corners of the Earth</em> is a first-person action-adventure survival horror game, based on the H.P. Lovecraft mythos and his short story ""The Shadow Over Innsmouth"". </p>
<p>Initially, <em>CoC: DCotE</em> plays like an adventure game, but soon it gains elements of a stealth game and of a first-person shooter. Notably, the game does not feature an on-screen HUD (not even a crosshair); Jack's health is hinted at by visual cues; as for ammo, you need to remember how much you have left before you'll have to reload. </p>
<p>The health system used in the game is uncommon. There is no ""hit points"" system; rather, Jack receives minor or major wounds in specific parts of the body, and if he breaks a leg he's slowed down. To heal himself and prevent death from bleeding out, Jack can pick up medikits which contain bandages, splints, sutures and antidotes, each of which is used to heal a specific type of wound. Ill effects emerging from the wounds can be temporarily suppressed with a fix of morphine. </p>
<p>Jack's sanity also plays an important role. When Jack looks at disturbing things or finds himself in alarming conditions, his vision blurs, he begins hearing voices and talking to himself. If this gets too bad, Jack may go insane or commit suicide. Also, Jack suffers from acrophobia, and looking down in high places will cause him vertigo.</p>", data.Description);
        Assert.Equal("https://cdn.mobygames.com/840ce008-abaf-11ed-8ed2-02420a0001a0.webp", data.CoverImage?.Path);
        Assert.Equal(77, data.CriticScore);
        Assert.Equal(76, data.CommunityScore);
        Assert.Contains(data.Links, l => l.Name == scraper.Name && l.Url == gameUrl);
        Assert.Contains(data.Links, l => l.Name == "Official Site" && l.Url == "http://www.callofcthulhu.com/");
        Assert.Contains(data.Links, l => l.Name == "Steam" && l.Url == "https://store.steampowered.com/app/22340/");
        Assert.Contains(data.Links, l => l.Name == "GOG" && l.Url == "https://www.gog.com/en/game/call_of_cthulhu_dark_corners_of_the_earth");
        Assert.Equal(2, webclient.CalledUrls.Count);
    }

    private static void ContainsProperty(HashSet<MetadataProperty> data, string expectedName)
    {
        MetadataNameProperty expectedProp = new(expectedName);
        Assert.Contains(expectedProp, data);
    }
}
