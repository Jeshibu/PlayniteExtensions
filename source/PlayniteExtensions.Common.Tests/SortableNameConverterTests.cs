using Xunit;

namespace PlayniteExtensions.Common.Tests
{
    public class SortableNameConverterTests
    {
        //Insignificant side-effects (harder to fix without making exceptions, and not damaging for sorting with other games in their franchise):

        //Back 4 Blood                                      ->      Back 04 Blood
        //Left 4 Dead                                       ->      Left 04 Dead
        //Spirit Swap: Lofi Beats to Match-3 To             ->      Spirit Swap: Lofi Beats to Match-03 To
        //Kingdom Hearts 358/2 Days                         ->      Kingdom Hearts 358/02 Days
        //Ether One                                         ->      Ether 01
        //It Takes Two                                      ->      It Takes 02
        //Army of Two	                                    ->      Army of 02
        //                                                          Army of Two: The Devil's Cartel won't be changed, but this will still preserve release order sorting
        //Hyperdimension Neptunia Re;Birth3 V Generation	->      Hyperdimension Neptunia Re;Birth3 05 Generation
        //STAR WARS: Rebel Assault I + II                   ->      STAR WARS: Rebel Assault I + 02
        //Emily is Away <3	                                ->      Emily is Away <03

        [Theory]
        [InlineData("Final Fantasy XIII-2", "Final Fantasy 13-02")]
        [InlineData("Final Fantasy Ⅻ", "Final Fantasy 12")] //Ⅻ is a single unicode character here
        [InlineData("FINAL FANTASY X/X-2 HD Remaster", "FINAL FANTASY 10/10-02 HD Remaster")]
        [InlineData("Warhammer ↂↇ", "Warhammer 40000")]
        [InlineData("Carmageddon 2: Carpocalypse Now", "Carmageddon 02: Carpocalypse Now")]
        [InlineData("SOULCALIBUR IV", "SOULCALIBUR 04")]
        [InlineData("Quake III: Team Arena", "Quake 03: Team Arena")]
        [InlineData("THE KING OF FIGHTERS XIV STEAM EDITION", "KING OF FIGHTERS 14 STEAM EDITION")]
        [InlineData("A Hat in Time", "Hat in Time")]
        [InlineData("Battlefield V", "Battlefield 05")]
        [InlineData("Tales of Monkey Island: Chapter 1 - Launch of the Screaming Narwhal", "Tales of Monkey Island: Chapter 01 - Launch of the Screaming Narwhal")]
        [InlineData("Tales of Monkey Island: Chapter I - Launch of the Screaming Narwhal", "Tales of Monkey Island: Chapter 01 - Launch of the Screaming Narwhal")]
        [InlineData("KOBOLD: Chapter I", "KOBOLD: Chapter 01")]
        [InlineData("Crazy Machines 1.5 New from the Lab", "Crazy Machines 01.5 New from the Lab")]
        [InlineData("Half-Life 2: Episode One", "Half-Life 02: Episode 01")]
        [InlineData("Unravel Two", "Unravel 02")]
        [InlineData("The Elder Scrolls II: Daggerfall Unity - GOG Cut", "Elder Scrolls 02: Daggerfall Unity - GOG Cut")]
        [InlineData("Metal Slug XX", "Metal Slug 20")]
        [InlineData("The Uncanny X-Men", "Uncanny X-Men")]
        [InlineData("Test X-", "Test 10-")]
        [InlineData("The Witcher 3", "Witcher 03")]
        [InlineData("the Witcher 3", "Witcher 03")]
        [InlineData("A Game", "Game")]
        [InlineData("An Usual Game", "Usual Game")]
        [InlineData("Title, The", "Title")]
        public void ConvertToSortableNameTest(string input, string expected)
        {
            var c = new SortableNameConverter();
            var output = c.Convert(input);
            Assert.Equal(expected, output);
        }

        [Theory]
        [InlineData("FINAL FANTASY X/X-2 HD Remaster", "FINAL FANTASY 10/10-02")]
        [InlineData("THE KING OF FIGHTERS XIV STEAM EDITION", "KING OF FIGHTERS 14")]
        [InlineData("The Elder Scrolls II: Daggerfall Unity - GOG Cut", "Elder Scrolls 02: Daggerfall Unity")]
        [InlineData("The Elder Scrolls IV: Oblivion - Game of the Year Edition Deluxe", "Elder Scrolls 04: Oblivion")]
        [InlineData("The Last Oricru – Final Cut", "Last Oricru")]
        [InlineData("BloodRayne: Terminal Cut", "BloodRayne")]
        [InlineData("Strike Suit Zero: Director's Cut", "Strike Suit Zero")]
        [InlineData("Lone Survivor: The Director's Cut", "Lone Survivor")]
        [InlineData("Divinity II: Developer's Cut", "Divinity 02")]
        [InlineData("Deadly Premonition: The Director's Cut", "Deadly Premonition")]
        [InlineData("Saints Row IV: Game of the Century Edition", "Saints Row 04")]
        [InlineData("POP: Methodology Experiment One - Game of The Saeculum Edition", "POP: Methodology Experiment 01")]
        [InlineData("Frog Fractions: Game of the Decade Edition", "Frog Fractions")]
        [InlineData("Apothecarium: The Renaissance of Evil Collector's Edition", "Apothecarium: The Renaissance of Evil")]
        [InlineData("Apothecarium: The Renaissance of Evil (Premium Edition)", "Apothecarium: The Renaissance of Evil")]
        [InlineData("Estigma [Steam Edition]", "Estigma")]
        [InlineData("Unsolved Mystery Club: Ancient Astronauts (Collector´s Edition)", "Unsolved Mystery Club: Ancient Astronauts")]
        [InlineData("Hop Step Sing! Kimamani☆Summer vacation (HQ Edition)", "Hop Step Sing! Kimamani☆Summer vacation")]
        [InlineData("Project Zero: Maiden of Black Water (Limited Edition)", "Project Zero: Maiden of Black Water")]
        public void RemoveEditionsTest(string input, string expected)
        {
            var c = new SortableNameConverter(removeEditions: true);
            var output = c.Convert(input);
            Assert.Equal(expected, output);
        }

        [Theory]
        [InlineData("Powerplay: The Game of the Gods")]
        [InlineData("Coma: Recut")]
        public void RemoveEditionsIsUnchanged(string input)
        {
            var c = new SortableNameConverter(removeEditions: true);
            var output = c.Convert(input);
            Assert.Equal(input, output);
        }

        [Theory]
        [InlineData("SHENZHEN I/O")]
        [InlineData("XIII")]
        [InlineData("X: Beyond the Frontier")]
        [InlineData("X3: Terran Conflict")]
        [InlineData("X-COM")]
        [InlineData("Gobliiins")]
        [InlineData("Before I Forget")]
        [InlineData("A.I.M. Racing")]
        [InlineData("S.T.A.L.K.E.R.: Shadow of Chernobyl")]
        [InlineData("Battlefield 1942")]
        [InlineData("Metal Wolf Chaos XD")]
        [InlineData("Prince of Persia: The Two Thrones")]
        [InlineData("Daemon X Machina")]
        [InlineData("Bit Blaster XL")]
        [InlineData("STAR WARS X-Wing vs TIE Fighter: Balance of Power Campaigns")]
        [InlineData("Star Wars: X-Wing Alliance")]
        [InlineData("Acceleration of Suguri X-Edition")]
        [InlineData("Guilty Gear X2 #Reload")]
        [InlineData("Mega Man X Legacy Collection")] //Mega Man 10 is a different game
        [InlineData("LEGO DC Super-Villains")]
        [InlineData("Constant C")]
        [InlineData("Metroid: Other M")]
        [InlineData("Zero Escape: Zero Time Dilemma")] //zero isn't currently parsed but if it ever is, this title should remain unchanged
        [InlineData("Worms W M D")]
        [InlineData("Sonic Adventure DX")]
        [InlineData("Zone of The Enders: The 2nd Runner M∀RS")]
        [InlineData("AnUsual Game")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void SortableNameIsUnchanged(string input)
        {
            var c = new SortableNameConverter();
            var output = c.Convert(input);
            Assert.Equal(input, output);
        }

        [Theory]
        [InlineData("The Witcher 3", "The Witcher 03")]
        [InlineData("A Hat in Time", "A Hat in Time")]
        public void SortableNameNoArticlesRemovedTest(string input, string expected)
        {
            var c = new SortableNameConverter(new string[0]);
            var output = c.Convert(input);
            Assert.Equal(expected, output);
        }

        [Theory]
        [InlineData("I", 1)]
        [InlineData("II", 2)]
        [InlineData("IV", 4)]
        [InlineData("VIII", 8)]
        [InlineData("IX", 9)]
        [InlineData("XIII", 13)]
        [InlineData("XIX", 19)]
        [InlineData("CCLXXXI", 281)]
        [InlineData("MCMLVIII", 1958)]
        [InlineData("LMMXXIV", 1974)]
        [InlineData("MLMXXIV", 1974)]
        [InlineData("MCMXCVIII", 1998)]
        [InlineData("MCMXCIX", 1999)]
        public void ConvertRomanNumeralsToIntTest(string input, int expected)
        {
            int? output = SortableNameConverter.ConvertRomanNumeralToInt(input);
            Assert.Equal(expected, output);
        }

        [Theory]
        [InlineData("IVX")]
        [InlineData("VIX")]
        [InlineData("IIII")]
        [InlineData("XXL")]
        [InlineData("IIX")]
        [InlineData("asdf")]
        public void ConvertRomanNumeralsToIntRejectsNonsense(string input)
        {
            int? output = SortableNameConverter.ConvertRomanNumeralToInt(input);
            Assert.Null(output);
        }
    }
}