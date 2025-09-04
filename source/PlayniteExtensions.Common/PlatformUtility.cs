using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PlayniteExtensions.Common;

public class PlatformUtility : IPlatformUtility
{
    private readonly IPlayniteAPI api;
    private Dictionary<string, string[]> platformSpecNameByNormalName;
    private HashSet<string> platformSpecNames;

    private Dictionary<string, string[]> PlatformSpecNameByNormalName => platformSpecNameByNormalName ??= GetPlatformSpecsByNormalName(api);

    private HashSet<string> PlatformSpecNames => platformSpecNames ??= api?.Database?.Platforms?.Select(p => p.SpecificationId).Where(x => x != null).ToHashSet() ?? [];

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
        if (!string.IsNullOrWhiteSpace(platformName) && specIds != null && specIds.Any())
            TryAddPlatformByName(platformSpecNameByNormalName, platformName, specIds);
    }

    private static readonly Regex TrimCompanyName = new(@"^(atari|bandai|coleco|commodore|mattel|nec|nintendo|sega|sinclair|snk|sony|microsoft)?\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex TrimInput = new(@"^(pal|jpn?|usa?|ntsc)\s+|[™®©]| version$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex TrimPlatformName = new(@"\s*(\((?<platform>[^()]+)\)|\[(?<platform>[^\[\]]+)\]|-\s+(?<platform>[a-z0-9 ]+))$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static Dictionary<string, string[]> GetPlatformSpecsByNormalName(IPlayniteAPI api)
    {
        var output = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);
        TryAddPlatformByName(output, ["3DO", "3DO Interactive Multiplayer"], "3do");
        TryAddPlatformByName(output, "Adobe", ["Flash"], "adobe_flash");
        TryAddPlatformByName(output, "Amstrad", ["CPC"], "amstrad_cpc");
        TryAddPlatformByName(output, ["Apple II", "Apple 2"], "apple_2");
        TryAddPlatformByName(output, "Atari", ["2600"], "atari_2600");
        TryAddPlatformByName(output, "Atari", ["5200"], "atari_5200");
        TryAddPlatformByName(output, "Atari", ["7800"], "atari_7800");
        TryAddPlatformByName(output, ["Atari 8-bit", "Atari 8bit", "Atari 8 bit"], "atari_8bit");
        TryAddPlatformByName(output, "Atari", ["Falcon030", "Falcon"], "atari_falcon030");
        TryAddPlatformByName(output, "Atari", ["Jaguar"], "atari_jaguar");
        TryAddPlatformByName(output, "Atari", ["Lynx"], "atari_lynx");
        TryAddPlatformByName(output, "Atari", ["ST", "STE"], "atari_st");
        TryAddPlatformByName(output, "Bandai", ["WonderSwan Color"], "bandai_wonderswan_color");
        TryAddPlatformByName(output, "Bandai", ["WonderSwan"], "bandai_wonderswan");
        TryAddPlatformByName(output, "Coleco", ["ColecoVision"], "coleco_vision");
        TryAddPlatformByName(output, ["Commodore 64", "Commodore 128", "Commodore 64/128"], "commodore_64");
        TryAddPlatformByName(output, "Commodore", ["Amiga"], "commodore_amiga");
        TryAddPlatformByName(output, "Commodore", ["Amiga CD32", "Amiga CD³²"], "commodore_amiga_cd32");
        TryAddPlatformByName(output, "Commodore", ["CBM-5x0"], "commodore_cbm5x0");
        TryAddPlatformByName(output, "Commodore", ["CBM-II"], "commodore_cbm2");
        TryAddPlatformByName(output, "Commodore", ["PET", "CBM"], "commodore_pet");
        TryAddPlatformByName(output, "Commodore 16", "commodore_plus4");
        TryAddPlatformByName(output, "Commodore", ["16, Plus/4", "Plus/4"], "commodore_plus4");
        TryAddPlatformByName(output, "Commodore", ["VIC20"], "commodore_vci20"); //typo in Playnite's Platforms.yaml
        TryAddPlatformByName(output, "GCE", ["Vectrex"], "vectrex");
        TryAddPlatformByName(output, "Apple", ["Mac", "Macintosh", "OSX"], "macintosh");
        TryAddPlatformByName(output, "Magnavox", ["Odyssey 2"], "magnavox_odyssey_2");
        TryAddPlatformByName(output, "Mattel", ["Intellivision"], "mattel_intellivision");
        TryAddPlatformByName(output, "Microsoft MSX", "microsoft_msx");
        TryAddPlatformByName(output, "MSX", "microsoft_msx", "microsoft_msx2");
        TryAddPlatformByName(output, "Microsoft", ["MSX2"], "microsoft_msx2");
        TryAddPlatformByName(output, "Microsoft", ["Xbox"], "xbox");
        TryAddPlatformByName(output, "Microsoft", ["Xbox 360", "Xbox360", "X360"], "xbox360");
        TryAddPlatformByName(output, "Microsoft", ["Xbox One", "XboxOne", "XONE"], "xbox_one");
        TryAddPlatformByName(output, "Microsoft", ["Xbox Series X", "Xbox Series S", "Xbox Series X/S", "Xbox Series S/X", "Xbox Series X|S", "XboxSeriesX", "XSX"], ["xbox_series"]);
        TryAddPlatformByName(output, "NEC", ["PC-98", "PC98", "PC-9801"], "nec_pc98");
        TryAddPlatformByName(output, "NEC", ["PC-FX", "PCFX"], "nec_pcfx");
        TryAddPlatformByName(output, "NEC", ["SuperGrafx"], "nec_supergrafx");
        TryAddPlatformByName(output, "NEC", ["TurboGrafx 16"], "nec_turbografx_16");
        TryAddPlatformByName(output, "NEC", ["TurboGrafx-CD", "TurboGrafx CD"], "nec_turbografx_cd");
        TryAddPlatformByName(output, "Nintendo", ["3DS"], "nintendo_3ds");
        TryAddPlatformByName(output, ["New Nintendo 3DS", "N3DS"], "nintendo_3ds"); //count it as part of 3DS for emulation purposes
        TryAddPlatformByName(output, ["Nintendo 64", "N64"], "nintendo_64");
        TryAddPlatformByName(output, "Nintendo", ["DS"], "nintendo_ds");
        TryAddPlatformByName(output, "Nintendo", ["DSi"], "nintendo_dsi");
        TryAddPlatformByName(output, ["Nintendo Entertainment System", "NES"], "nintendo_nes");
        TryAddPlatformByName(output, "Nintendo", ["Family Computer Disk System", "Famicom Disk System"], "nintendo_famicom_disk");
        TryAddPlatformByName(output, "Nintendo", ["Game Boy Advance", "Gameboy Advance", "GBA"], "nintendo_gameboyadvance");
        TryAddPlatformByName(output, "Nintendo", ["Game Boy Color", "Gameboy Color", "GBC"], "nintendo_gameboycolor");
        TryAddPlatformByName(output, "Nintendo", ["Game Boy", "Gameboy", "GB"], "nintendo_gameboy");
        TryAddPlatformByName(output, "Nintendo", ["GameCube"], "nintendo_gamecube");
        TryAddPlatformByName(output, "Nintendo", ["Super NES", "SNES"], "nintendo_super_nes");
        TryAddPlatformByName(output, "Super Nintendo Entertainment System", "nintendo_super_nes");
        TryAddPlatformByName(output, "Nintendo", ["Switch"], "nintendo_switch");
        TryAddPlatformByName(output, "Nintendo", ["Switch 2"], "nintendo_switch2");
        TryAddPlatformByName(output, "Nintendo", ["Virtual Boy"], "nintendo_virtualboy");
        TryAddPlatformByName(output, "Nintendo", ["Wii U", "WiiU"], "nintendo_wiiu");
        TryAddPlatformByName(output, ["PC (DOS)", "DOS", "MS-DOS"], "pc_dos");
        TryAddPlatformByName(output, ["PC (Linux)", "Linux", "LIN"], "pc_linux");
        TryAddPlatformByName(output, ["PC (Windows)", "Windows", "PC", "PC CD-ROM", "PC DVD", "PC DVD-ROM", "Windows Apps", "win", "Windows 3.x"], "pc_windows");
        TryAddPlatformByName(output, "Sega", ["32X"], "sega_32x");
        TryAddPlatformByName(output, "Sega CD", "sega_cd");
        TryAddPlatformByName(output, "Sega", ["Dreamcast", "DC"], "sega_dreamcast");
        TryAddPlatformByName(output, "Sega", ["Game Gear"], "sega_gamegear");
        TryAddPlatformByName(output, "Sega", ["Genesis"], "sega_genesis");
        TryAddPlatformByName(output, "Sega", ["Master System"], "sega_mastersystem");
        TryAddPlatformByName(output, "Sega", ["Saturn"], "sega_saturn");
        TryAddPlatformByName(output, "Sega", ["SG-1000"], "sega_sg1000");
        TryAddPlatformByName(output, "Sharp", ["X68000"], "sharp_x68000");
        TryAddPlatformByName(output, "Sinclair", ["ZX Spectrum"], "sinclair_zxspectrum");
        TryAddPlatformByName(output, "Sinclair", ["ZX Spectrum +3"], "sinclair_zxspectrum3");
        TryAddPlatformByName(output, "Sinclair", ["ZX81"], "sinclair_zx81");
        TryAddPlatformByName(output, "SNK", ["Neo Geo AES"], "snk_neogeo_aes");
        TryAddPlatformByName(output, "SNK", ["Neo Geo CD"], "snk_neogeo_cd");
        TryAddPlatformByName(output, "SNK", ["Neo Geo Pocket Color"], "snk_neogeopocket_color");
        TryAddPlatformByName(output, "SNK", ["Neo Geo Pocket"], "snk_neogeopocket");
        TryAddPlatformByName(output, "Sony", ["Playstation", "PS", "PS1", "PSX"], "sony_playstation");
        TryAddPlatformByName(output, "Sony", ["Playstation 2", "PS2"], "sony_playstation2");
        TryAddPlatformByName(output, "Sony", ["Playstation 3", "PS3"], "sony_playstation3");
        TryAddPlatformByName(output, "Sony", ["Playstation 4", "PS4"], "sony_playstation4");
        TryAddPlatformByName(output, "Sony", ["Playstation 5", "PS5"], "sony_playstation5");
        TryAddPlatformByName(output, "Sony", ["Playstation Portable", "PSP"], "sony_psp");
        TryAddPlatformByName(output, "Sony", ["Playstation Vita", "PS Vita", "Vita"], "sony_vita");
        TryAddPlatformByName(output, "Sony", ["PS4/5", "Playstation 4/5"], ["sony_playstation4", "sony_playstation5"]);
        TryAddPlatformByName(output, "Texas Instruments", ["TI-83"], "ti_83");
        TryAddPlatformByName(output, "Thomson", ["MO5"], "thomson_mo5");
        TryAddPlatformByName(output, "Thomson", ["TO7"], "thomson_to7");
        TryAddPlatformByName(output, "TIC-80", "tic_80");
        TryAddPlatformByName(output, "Uzebox", "uzebox");
        TryAddPlatformByName(output, "Watara", ["Supervision"], "watara_supervision");
        TryAddPlatformByName(output, "Philips", ["CD-i", "CDI"], "philips_cdi");
        TryAddPlatformByName(output, ["Pokémon mini", "Pokemon mini"], "pokemon_mini");
        TryAddPlatformByName(output, "AMI", "commodore_amiga");
        TryAddPlatformByName(output, "GG", "sega_gamegear");
        TryAddPlatformByName(output, "GEN", "sega_genesis");
        TryAddPlatformByName(output, "LYNX", "atari_lynx");
        TryAddPlatformByName(output, "SMS", "sega_mastersystem");
        TryAddPlatformByName(output, "APL2", "apple_2");
        TryAddPlatformByName(output, "AST", "atari_st");
        TryAddPlatformByName(output, "C64", "commodore_64");
        TryAddPlatformByName(output, "SPEC", "sinclair_zxspectrum");
        TryAddPlatformByName(output, "GCN", "nintendo_gamecube");
        TryAddPlatformByName(output, "A800", "atari_8bit");
        TryAddPlatformByName(output, "NEO", "snk_neogeo_aes");
        TryAddPlatformByName(output, "JAG", "atari_jaguar");
        TryAddPlatformByName(output, "SCD", "sega_cd");
        TryAddPlatformByName(output, "VC20", "commodore_vci20"); //typo in Playnite's Platforms.yaml
        TryAddPlatformByName(output, "CD32", "commodore_amiga_cd32");
        TryAddPlatformByName(output, "SAT", "sega_saturn");
        TryAddPlatformByName(output, "CVIS", "coleco_vision");
        TryAddPlatformByName(output, "INTV", "mattel_intellivision");
        TryAddPlatformByName(output, "DS", "nintendo_ds");
        TryAddPlatformByName(output, "TGCD", "nec_turbografx_cd");
        TryAddPlatformByName(output, "WSC", "bandai_wonderswan_color");
        TryAddPlatformByName(output, "TG16", "nec_turbografx_16");
        TryAddPlatformByName(output, "NGCD", "snk_neogeo_cd");
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
        TryAddPlatformByName(output, "SGFX", "nec_supergrafx");
        TryAddPlatformByName(output, "SG1K", "sega_sg1000");
        TryAddPlatformByName(output, "SVIS", "watara_supervision");
        TryAddPlatformByName(output, "NSW", "nintendo_switch");

        if (api?.Database?.Platforms == null) //for use in unit tests so you don't have to instantiate the entire database or platform list
            return output;

        foreach (var platform in api.Database.Platforms)
        {
            if (platform.SpecificationId == null)
                continue;

            if (!output.ContainsKey(platform.Name))
                output.Add(platform.Name, [platform.SpecificationId]);

            string nameWithoutCompany = TrimCompanyName.Replace(platform.Name, string.Empty);
            if (!output.ContainsKey(nameWithoutCompany))
                output.Add(nameWithoutCompany, [platform.SpecificationId]);
        }
        return output;
    }

    private static readonly Dictionary<string, string> nameAbbreviations = new(StringComparer.InvariantCulture)
    {
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
        { "MQST", "Meta Quest" },
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
        { "Quest", "Meta Quest" },
        { "Oculus Quest", "Meta Quest" },
    };

    private static bool TryAddPlatformByName(Dictionary<string, string[]> dict, string platformName, params string[] platformSpecNames)
    {
        if (dict.ContainsKey(platformName))
            return false;

        dict.Add(platformName, platformSpecNames);
        return true;
    }

    private static bool TryAddPlatformByName(Dictionary<string, string[]> dict, string[] platformNames, params string[] platformSpecNames) => TryAddPlatformByName(dict, null, platformNames, platformSpecNames);

    private static bool TryAddPlatformByName(Dictionary<string, string[]> dict, string companyName, string[] platformNames, params string[] platformSpecNames)
    {
        bool success = true;
        List<string> namePrefixes = [""];
        if (!string.IsNullOrEmpty(companyName))
            namePrefixes.Add(companyName + ' ');

        foreach (var prefix in namePrefixes)
            foreach (var platformName in platformNames)
                success &= TryAddPlatformByName(dict, prefix + platformName, platformSpecNames);

        return success;
    }

    public IEnumerable<MetadataProperty> GetPlatforms(string platformName) => GetPlatforms(platformName, strict: false);

    public IEnumerable<MetadataProperty> GetPlatforms(string platformName, bool strict)
    {
        if (string.IsNullOrWhiteSpace(platformName))
            return [];

        string sanitizedPlatformName = TrimInput.Replace(platformName, string.Empty);

        if (PlatformSpecNameByNormalName.TryGetValue(sanitizedPlatformName, out string[] specIds))
            return specIds.Select(s => new MetadataSpecProperty(s)).ToList<MetadataProperty>();

        if (nameAbbreviations.TryGetValue(sanitizedPlatformName, out string foundPlatformName))
            return [new MetadataNameProperty(foundPlatformName)];

        if (PlatformSpecNames.Contains(platformName))
            return [new MetadataSpecProperty(platformName)];

        if (strict)
            return [];
        else
            return [new MetadataNameProperty(sanitizedPlatformName)];
    }

    public IEnumerable<string> GetPlatformNames()
    {
        return PlatformSpecNameByNormalName.Keys;
    }

    public IEnumerable<MetadataProperty> GetPlatformsFromName(string name, out string trimmedName)
    {
        IEnumerable<MetadataProperty> platforms = [];
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
