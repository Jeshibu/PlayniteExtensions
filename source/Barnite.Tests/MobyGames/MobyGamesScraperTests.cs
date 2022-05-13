using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.IO;
using Xunit;

namespace Barnite.Tests.MobyGames
{
    public class MobyGamesScraperTests
    {
        [Fact]
        public void ScrapingCallOfCthulhuReturnsCorrectMetadata()
        {
            var webclient = new FakeWebDownloader("https://www.mobygames.com/search/quick?q=093155118706", "./MobyGames/coc.html");
            var scraper = new MobyGamesScraper();
            scraper.Initialize(new PlatformUtility("Xbox", "xbox"), webclient);

            var data = scraper.GetMetadataFromBarcode("093155118706");

            Assert.Equal("Call of Cthulhu: Dark Corners of the Earth", data.Name);
            Assert.Single(data.Platforms, new MetadataSpecProperty("xbox"));
            Assert.Contains(new MetadataNameProperty("2K Games, Inc."), data.Publishers);
            Assert.Contains(new MetadataNameProperty("Bethesda Softworks LLC"), data.Publishers);
            Assert.Single(data.Developers, new MetadataNameProperty("Headfirst Productions"));
            Assert.Equal(new ReleaseDate(2005, 10, 24), data.ReleaseDate);
            Assert.Single(data.AgeRatings, new MetadataNameProperty("ESRB Mature"));
            Assert.Contains(new MetadataNameProperty("Action"), data.Genres);
            Assert.Contains(new MetadataNameProperty("1st-person"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Stealth"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Survival horror"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Detective / mystery"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Horror"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Interwar"), data.Tags);
            Assert.Contains(new MetadataNameProperty("Licensed"), data.Tags);
            Assert.Contains(new MetadataNameProperty("Regional differences"), data.Tags);
            Assert.Equal("Detective Jack Walters arrived in Innsmouth to solve a case of a missing person. But soon he finds himself confronted with terrible mysteries older than humanity, and with ghosts of the mysterious events that led to his incarceration in a mental hospital years ago. <br><br><i>Call of Cthulhu: Dark Corners of the Earth</i> is a first-person action-adventure survival horror game, based on the H.P. Lovecraft mythos and his short story &quot;The Shadow Over Innsmouth&quot;. <br><br>Initially, <i>CoC: DCotE</i> plays like an adventure game, but soon it gains elements of a stealth game and of a first-person shooter. Notably, the game does not feature an on-screen HUD (not even a crosshair); Jack's health is hinted at by visual cues; as for ammo, you need to remember how much you have left before you'll have to reload. <br><br>The health system used in the game is uncommon. There is no &quot;hit points&quot; system; rather, Jack receives minor or major wounds in specific parts of the body, and if he breaks a leg he's slowed down. To heal himself and prevent death from bleeding out, Jack can pick up medikits which contain bandages, splints, sutures and antidotes, each of which is used to heal a specific type of wound. Ill effects emerging from the wounds can be temporarily suppressed with a fix of morphine. <br><br>Jack's sanity also plays an important role. When Jack looks at disturbing things or finds himself in alarming conditions, his vision blurs, he begins hearing voices and talking to himself. If this gets too bad, Jack may go insane or commit suicide. Also, Jack suffers from acrophobia, and looking down in high places will cause him vertigo.", data.Description);
            Assert.Equal("https://www.mobygames.com/images/covers/l/56836-call-of-cthulhu-dark-corners-of-the-earth-xbox-front-cover.jpg", data.CoverImage?.Path);
            Assert.Contains(data.Links, l => l.Name == scraper.Name);
            Assert.Single(webclient.CalledUrls);
        }
    }
}
