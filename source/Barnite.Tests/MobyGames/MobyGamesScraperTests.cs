using Barnite.Scrapers;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Tests.Common;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Barnite.Tests.MobyGames
{
    public class MobyGamesScraperTests
    {
        [Fact]
        public void ScrapingCallOfCthulhuReturnsCorrectMetadata()
        {
            var webclient = new FakeWebDownloader(new Dictionary<string, string> {
                { "https://www.mobygames.com/search/?q=093155118706&type=game", "./MobyGames/cocsearch.html" },
                { "https://www.mobygames.com/game/20705/call-of-cthulhu-dark-corners-of-the-earth/", "./MobyGames/coc.html" }
            });
            var scraper = new MobyGamesScraper();
            scraper.Initialize(new PlatformUtility("Xbox", "xbox"), webclient);

            var data = scraper.GetMetadataFromBarcode("093155118706");

            Assert.NotNull(data);
            Assert.Equal("Call of Cthulhu: Dark Corners of the Earth", data.Name);
            Assert.Single(data.Platforms, new MetadataSpecProperty("xbox"));
            Assert.Contains(new MetadataNameProperty("2K Games"), data.Publishers);
            Assert.Contains(new MetadataNameProperty("Bethesda Softworks"), data.Publishers);
            Assert.Single(data.Developers, new MetadataNameProperty("Headfirst Productions"));
            Assert.Equal(new ReleaseDate(2005, 10, 24), data.ReleaseDate);
            Assert.Contains(new MetadataNameProperty("Action"), data.Genres);
            Assert.Contains(new MetadataNameProperty("1st-person"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Stealth"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Survival horror"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Detective / mystery"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Horror"), data.Genres);
            Assert.Contains(new MetadataNameProperty("Interwar"), data.Tags);
            Assert.Contains(new MetadataNameProperty("Licensed"), data.Tags);
            Assert.Contains(new MetadataNameProperty("Regional differences"), data.Tags);
            Assert.Equal(@"<p>Detective Jack Walters arrived in Innsmouth to solve a case of a missing person. But soon he finds himself confronted with terrible mysteries older than humanity, and with ghosts of the mysterious events that led to his incarceration in a mental hospital years ago. </p>
<p><em>Call of Cthulhu: Dark Corners of the Earth</em> is a first-person action-adventure survival horror game, based on the H.P. Lovecraft mythos and his short story ""The Shadow Over Innsmouth"". </p>
<p>Initially, <em>CoC: DCotE</em> plays like an adventure game, but soon it gains elements of a stealth game and of a first-person shooter. Notably, the game does not feature an on-screen HUD (not even a crosshair); Jack's health is hinted at by visual cues; as for ammo, you need to remember how much you have left before you'll have to reload. </p>
<p>The health system used in the game is uncommon. There is no ""hit points"" system; rather, Jack receives minor or major wounds in specific parts of the body, and if he breaks a leg he's slowed down. To heal himself and prevent death from bleeding out, Jack can pick up medikits which contain bandages, splints, sutures and antidotes, each of which is used to heal a specific type of wound. Ill effects emerging from the wounds can be temporarily suppressed with a fix of morphine. </p>
<p>Jack's sanity also plays an important role. When Jack looks at disturbing things or finds himself in alarming conditions, his vision blurs, he begins hearing voices and talking to himself. If this gets too bad, Jack may go insane or commit suicide. Also, Jack suffers from acrophobia, and looking down in high places will cause him vertigo.</p>".Replace("\r", ""), data.Description);
            Assert.Equal("https://cdn.mobygames.com/840ce008-abaf-11ed-8ed2-02420a0001a0.webp", data.CoverImage?.Path);
            Assert.Contains(data.Links, l => l.Name == scraper.Name);
            Assert.Equal(2, webclient.CalledUrls.Count);
        }
    }
}
