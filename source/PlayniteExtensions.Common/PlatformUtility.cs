using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace PlayniteExtensions.Common
{
    public class PlatformUtility : IPlatformUtility
    {
        private readonly IPlayniteAPI api;
        private Dictionary<string, string[]> platformSpecNameByNormalName;
        private Dictionary<string, string[]> PlatformSpecNameByNormalName
        {
            get
            {
                return platformSpecNameByNormalName ?? (platformSpecNameByNormalName = GetPlatformSpecsByNormalName(api));
            }
        }

        public PlatformUtility(IPlayniteAPI api)
        {
            this.api = api;
        }

        /// <summary>
        /// For unit testing purposes only
        /// </summary>
        /// <param name="overrideValues"></param>
        public PlatformUtility(Dictionary<string, string[]> overrideValues)
        {
            platformSpecNameByNormalName = overrideValues;
        }

        /// <summary>
        /// For unit testing purposes only
        /// </summary>
        /// <param name="platformName"></param>
        /// <param name="specId"></param>
        public PlatformUtility(string platformName, params string[] specIds)
        {
            platformSpecNameByNormalName = GetPlatformSpecsByNormalName(null);
            if(!string.IsNullOrWhiteSpace(platformName) && specIds != null && specIds.Any())
                TryAddPlatformByName(platformSpecNameByNormalName, platformName, specIds);
        }

        private static Regex TrimCompanyName = new Regex(@"^(atari|bandai|coleco|commodore|mattel|nec|nintendo|sega|sinclair|snk|sony|microsoft)?\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex TrimInput = new Regex(@"^(pal|jpn?|usa?|ntsc)\s+|[™®©]| version$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex TrimPlatformName = new Regex(@"\s*(\((?<platform>[^()]+)\)|\[(?<platform>[^\[\]]+)\]|-\s+(?<platform>[a-z0-9 ]+))$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static Dictionary<string, string[]> GetPlatformSpecsByNormalName(IPlayniteAPI api)
        {
            var output = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);
            if (api?.Database?.Platforms != null) //for use in unit tests so you don't have to instantiate the entire database or platform list
            {
                foreach (var platform in api.Database.Platforms)
                {
                    if (platform.SpecificationId == null)
                        continue;

                    if (!output.ContainsKey(platform.Name))
                        output.Add(platform.Name, new[] { platform.SpecificationId });

                    string nameWithoutCompany = TrimCompanyName.Replace(platform.Name, string.Empty);
                    if (!output.ContainsKey(nameWithoutCompany))
                        output.Add(nameWithoutCompany, new[] { platform.SpecificationId });
                }
            }
            TryAddPlatformByName(output, "3DO", "3do");
            TryAddPlatformByName(output, new[] { "Windows", "PC", "PC CD-ROM", "PC DVD", "PC DVD-ROM", "Windows Apps" }, new[] { "pc_windows" });
            TryAddPlatformByName(output, new[] { "DOS", "MS-DOS" }, "pc_dos");
            TryAddPlatformByName(output, new[] { "Linux", "LIN" }, "pc_linux");
            TryAddPlatformByName(output, new[] { "Mac", "OSX" }, "macintosh");
            TryAddPlatformByName(output, new[] { "Xbox360", "X360" }, new[] { "xbox360" });
            TryAddPlatformByName(output, new[] { "XboxOne", "XONE" }, new[] { "xbox_one" });
            TryAddPlatformByName(output, new[] { "Microsoft Xbox Series X", "Microsoft Xbox Series S", "Xbox Series X", "Xbox Series S", "Microsoft Xbox Series X/S", "Microsoft Xbox Series S/X", "Xbox Series X/S", "Xbox Series S/X", "Xbox Series X|S", "XboxSeriesX", "XSX" }, new[] { "xbox_series" });
            TryAddPlatformByName(output, new[] { "PS", "PS1", "PSX" }, new[] { "sony_playstation" });
            TryAddPlatformByName(output, "PS2", "sony_playstation2");
            TryAddPlatformByName(output, "PS3", "sony_playstation3");
            TryAddPlatformByName(output, "PS4", "sony_playstation4");
            TryAddPlatformByName(output, "PS5", "sony_playstation5");
            TryAddPlatformByName(output, "PSP", "sony_psp");
            TryAddPlatformByName(output, new[] { "PS Vita", "Vita" }, "sony_vita");
            TryAddPlatformByName(output, new[] { "PS4/5", "Playstation 4/5" }, new[] { "sony_playstation4", "sony_playstation5" });
            TryAddPlatformByName(output, "Commodore 64/128", "commodore_64");
            TryAddPlatformByName(output, "AMI", "commodore_amiga");
            TryAddPlatformByName(output, "GB", "nintendo_gameboy");
            TryAddPlatformByName(output, "GBA", "nintendo_gameboyadvance");
            TryAddPlatformByName(output, "GG", "sega_gamegear");
            TryAddPlatformByName(output, "GEN", "sega_genesis");
            TryAddPlatformByName(output, "LYNX", "atari_lynx");
            TryAddPlatformByName(output, "SMS", "sega_mastersystem");
            TryAddPlatformByName(output, new[] { "SNES", "Super Nintendo Entertainment System" }, "nintendo_super_nes");
            TryAddPlatformByName(output, "APL2", "apple_2");
            TryAddPlatformByName(output, "AST", "atari_st");
            TryAddPlatformByName(output, "C64", "commodore_64");
            TryAddPlatformByName(output, "MSX", "microsoft_msx", "microsoft_msx2");
            TryAddPlatformByName(output, "SPEC", "sinclair_zxspectrum");
            TryAddPlatformByName(output, "NES", "nintendo_nes");
            TryAddPlatformByName(output, "GCN", "nintendo_gamecube");
            TryAddPlatformByName(output, "A800", "atari_8bit");
            TryAddPlatformByName(output, "NEO", "snk_neogeo_aes");
            TryAddPlatformByName(output, "JAG", "atari_jaguar");
            TryAddPlatformByName(output, "SCD", "sega_cd");
            TryAddPlatformByName(output, "VC20", "commodore_vci20"); //typo in Playnite's Platforms.yaml
            TryAddPlatformByName(output, "32X", "sega_32x");
            TryAddPlatformByName(output, "DC", "sega_dreamcast");
            TryAddPlatformByName(output, "CD32", "commodore_amiga_cd32");
            TryAddPlatformByName(output, "SAT", "sega_saturn");
            TryAddPlatformByName(output, "N64", "nintendo_64");
            TryAddPlatformByName(output, "CVIS", "coleco_vision");
            TryAddPlatformByName(output, "INTV", "mattel_intellivision");
            TryAddPlatformByName(output, "DS", "nintendo_ds");
            TryAddPlatformByName(output, "TGCD", "nec_turbografx_cd");
            TryAddPlatformByName(output, "WSC", "bandai_wonderswan_color");
            TryAddPlatformByName(output, "TG16", "nec_turbografx_16");
            TryAddPlatformByName(output, "GBC", "nintendo_gameboycolor");
            TryAddPlatformByName(output, "NGCD", "snk_neogeo_cd");
            TryAddPlatformByName(output, "CBM", "commodore_pet");
            TryAddPlatformByName(output, "WSW", "bandai_wonderswan");
            TryAddPlatformByName(output, "2600", "atari_2600");
            TryAddPlatformByName(output, "5200", "atari_5200");
            TryAddPlatformByName(output, "7800", "atari_7800");
            TryAddPlatformByName(output, "PCFX", "nec_pcfx");
            TryAddPlatformByName(output, "VECT", "vectrex");
            TryAddPlatformByName(output, "VBOY", "nintendo_virtualboy");
            TryAddPlatformByName(output, "NGP", "snk_neogeopocket");
            TryAddPlatformByName(output, "NGPC", "snk_neogeopocket_color");
            TryAddPlatformByName(output, "FDS", "nintendo_famicom_disk");
            TryAddPlatformByName(output, "X68K", "sharp_x68000");
            TryAddPlatformByName(output, "3DS", "nintendo_3ds");
            TryAddPlatformByName(output, "SGFX", "nec_supergrafx");
            TryAddPlatformByName(output, "WiiU", "nintendo_wiiu");
            TryAddPlatformByName(output, "SG1K", "sega_sg1000");
            TryAddPlatformByName(output, "SVIS", "watara_supervision");
            TryAddPlatformByName(output, "N3DS", "nintendo_3ds"); //New Nintendo 3DS, count it as part of 3DS for emulation purposes
            TryAddPlatformByName(output, "NSW", "nintendo_switch");
            return output;
        }

        private static Dictionary<string, string> nameAbbreviations = new Dictionary<string, string>(StringComparer.InvariantCulture)
        {
            { "CDI", "Philips CD-i" },
            { "NGE", "Nokia N-Gage" },
            { "A2GS", "Apple IIgs" },
            { "TI99", "TI-99/4A" },
            { "C128", "Commodore 128" },
            { "ODY2", "Magnavox Odyssey 2" },
            { "DRAG", "Dragon 32/64" },
            { "TRS8", "Tandy TRS-80" },
            { "ZOD", "Tapwave Zodiac" },
            { "CHNF", "Fiarchild Channel F" },
            { "COCO", "Tandy TRS-80 CoCo" },
            { "IPOD", "Apple iPod" },
            { "ODYS", "Magnavox Odyssey" },
            { "GCOM", "Game.Com" },
            { "GIZ", "Gizmondo" },
            { "VSML", "Vtech V.Smile" },
            { "PIN", "Pinball" },
            { "ARC", "Arcade" },
            { "NUON", "VMI NUON" },
            { "XBGS", "Xbox 360 Games Store" },
            { "WSHP", "Nintendo Wii Shop" },
            { "PS3N", "PlayStation Network (PS3)" },
            { "LEAP", "Leapster" },
            { "MVIS", "Microvision" },
            { "LACT", "Pioneer LaserActive" },
            { "AVIS", "Entex Adventure Vision" },
            { "IPHN", "Apple iPhone" },
            { "BS-X", "Nintendo Satellaview" },
            { "A2K1", "Emerson Arcadia 2001" },
            { "AQUA", "Mattel Aquarius" },
            { "64DD", "Nintendo 64DD" },
            { "PIPN", "Bandai Pippin" },
            { "RZON", "Tiger R-Zone" },
            { "HSCN", "Mattel HyperScan" },
            { "GWAV", "Game Wave" },
            { "DSI", "Nintendo DSiWare" },
            { "HALC", "RDI Halcyon" },
            { "FMT", "Fujitsu FM Towns" },
            { "PC88", "NEC PC-8801" },
            { "BBCM", "BBC Micro" },
            { "PLTO", "PLATO" },
            { "PC98", "NEC PC-9801" },
            { "X1", "Sharp X1" },
            { "FM7", "Fujitsu Micro 7" },
            { "6001", "NEC PC-6001" },
            { "PSPN", "PlayStation Network (PSP)" },
            { "PICO", "Sega Pico" },
            { "BAST", "Bally Astrocade" },
            { "IPAD", "Apple iPad" },
            { "ZBO", "Zeebo" },
            { "ANDR", "Google Android" },
            { "WP", "Windows Phone" },
            { "ACRN", "Acorn Archimedes" },
            { "LOOP", "Casio Loopy" },
            { "PDIA", "Bandai Playdia" },
            { "MZ", "Bandai MZ" },
            { "RCA2", "RCA Studio II" },
            { "XAVX", "XaviXPORT" },
            { "GP32", "GamePark 32" },
            { "PMIN", "Nintendo Pokémon mini" },
            { "CASV", "Epoch Cassette Vision" },
            { "SCV", "Epoch Super Cassette Vision" },
            { "3DSE", "Nintendo 3DS eShop" },
            { "BROW", "Browser" },
            { "CDTV", "Commodore CDTV" },
            { "PSNV", "PlayStation Network (Vita)" },
            { "DIDJ", "Leapfrog Didj" },
            { "AMAX", "Action Max" },
            { "PV1K", "Casio PV-1000" },
            { "C16", "Commodore 16" },
            { "ACAN", "Super A'Can" },
            { "VIS", "Memorex MD 2500 VIS" },
            { "OUYA", "Ouya" },
            { "FIRE", "Amazon Fire TV" },
            { "HGM", "Hartung Game Master" },
            { "APTV", "Apple TV" },
            { "SMC7", "Sony SMC-777" },
            { "COUP", "SAM Coupé" },
            { "VMIV", "View-Master Interactive Vision" },
            { "TF1", "Fuze Tomahawk F1" },
            { "TUT", "Tomy Tutor" },
            { "GMT", "Gamate" },
            { "MBEE", "MicroBee" },
            { "VSOC", "VTech Socrates" },
            { "ABC", "Luxor ABC80" },
            { "JCD", "Atari Jaguar CD" },
            { "ALXA", "Amazon Alexa" },
            { "ML1", "Magic Leap One" },
            { "BNA", "Advanced Pico Beena" },
            { "STAD", "Google Stadia" },
            { "MQST", "Oculus Quest" },
            { "PLDT", "Playdate" },
            { "EVER", "Evercade" },
            { "AMIC", "Intellivision Amico" },
            { "EPOC", "Epoch Game Pocket Computer" },
            { "SMAK", "Smaky" },
            { "ESAG", "Entex Select-A-Game" },
            { "LEXP", "Leapster Explorer" },
            { "LPAD", "LeapPad" },
            { "PV2K", "Casio PV-2000" },
            { "GK1", "Timetop GameKing" },
            { "GK3", "Timetop GameKing III" },
            { "MP1K", "APF MP-1000" },
            { "VC4K", "Interton VC 4000" },
            { "DGBL", "Digiblast" },
            { "M5", "Sord M5" },
            { "STRM", "Stream" },
            { "ORIC", "Oric" },
            { "TVB", "Gakken Compact Vision TV Boy" },
            { "PCW", "Amstrad PCW" },
            { "HS1", "Hitachi S1" },
            { "LUNA", "Amazon Luna" },
            { "DM", "Denshi Mangajuku" },
            { "RX78", "Bandai RX-78" },
            { "AVCS", "Atari VCS" },
            { "M2", "3DO M2" },
            { "MC", "Monon Color" },
            { "MM8", "Mitsubishi Multi 8" },
            { "MTX", "Memotech MTX" },
            { "ADAM", "Coleco Adam" },
            { "DVD", "DVD" },
            { "VTCV", "VTech CreatiVision" },
        };

        private static bool TryAddPlatformByName(Dictionary<string, string[]> dict, string platformName, params string[] platformSpecNames)
        {
            if (dict.ContainsKey(platformName))
                return false;

            dict.Add(platformName, platformSpecNames);
            return true;
        }

        private static bool TryAddPlatformByName(Dictionary<string, string[]> dict, string[] platformNames, params string[] platformSpecNames)
        {
            bool success = true;
            foreach (var platformName in platformNames)
            {
                success &= TryAddPlatformByName(dict, platformName, platformSpecNames);
            }
            return success;
        }

        public IEnumerable<MetadataProperty> GetPlatforms(string platformName)
        {
            return GetPlatforms(platformName, strict: false);
        }

        public IEnumerable<MetadataProperty> GetPlatforms(string platformName, bool strict)
        {
            if (string.IsNullOrWhiteSpace(platformName))
                return new List<MetadataProperty>();

            string sanitizedPlatformName = TrimInput.Replace(platformName, string.Empty);
            string companyTrimmedPlatformName = TrimCompanyName.Replace(sanitizedPlatformName, string.Empty);

            if (PlatformSpecNameByNormalName.TryGetValue(sanitizedPlatformName, out string[] specIds)
                || PlatformSpecNameByNormalName.TryGetValue(companyTrimmedPlatformName, out specIds))
                return specIds.Select(s => new MetadataSpecProperty(s)).ToList<MetadataProperty>();

            if (nameAbbreviations.TryGetValue(sanitizedPlatformName, out string foundPlatformName))
                return new[] { new MetadataNameProperty(foundPlatformName) };

            if (strict)
                return new List<MetadataProperty>();
            else
                return new List<MetadataProperty> { new MetadataNameProperty(sanitizedPlatformName) };
        }

        public IEnumerable<string> GetPlatformNames()
        {
            return PlatformSpecNameByNormalName.Keys;
        }

        public IEnumerable<MetadataProperty> GetPlatformsFromName(string name, out string trimmedName)
        {
            IEnumerable<MetadataProperty> platforms = new MetadataProperty[0];
            trimmedName = TrimPlatformName.Replace(name, match =>
            {
                var platformName = match.Groups["platform"].Value;
                platforms = GetPlatforms(platformName, strict: true);

                if (platforms.Any())
                    return string.Empty;
                else
                    return match.Value;
            });
            return platforms;
        }

        public bool PlatformsOverlap(List<Platform> platforms, List<MetadataProperty> metadataPlatforms, bool returnValueWhenEmpty = true)
        {
            if (platforms?.Any() != true || metadataPlatforms?.Any() != true)
                return returnValueWhenEmpty;

            var comparer = new TitleComparer();

            foreach (var mp in metadataPlatforms)
            {
                if (mp is MetadataSpecProperty specPlatform && platforms.Any(p => specPlatform.Id == p.SpecificationId))
                    return true;

                if (mp is MetadataNameProperty namePlatform && platforms.Select(p => p.Name).Contains(namePlatform.Name, comparer))
                    return true;
            }
            return false;
        }

        public bool PlatformsOverlap(List<Platform> platforms, IEnumerable<string> metadataPlatforms, bool returnValueWhenEmpty = true)
        {
            var parsedMetadataPlatforms = metadataPlatforms.SelectMany(p => GetPlatforms(p)).ToList();
            return PlatformsOverlap(platforms, parsedMetadataPlatforms, returnValueWhenEmpty);
        }
    }

}
