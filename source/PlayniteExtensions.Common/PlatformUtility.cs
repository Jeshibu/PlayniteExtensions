using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
            platformSpecNameByNormalName = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase) { { platformName, specIds } };
        }

        private static Regex TrimCompanyName = new Regex(@"^(atari|bandai|coleco|commodore|mattel|nec|nintendo|sega|sinclair|snk|sony|microsoft)?\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex TrimInput = new Regex(@"^(pal|jpn?|usa?|ntsc)\s+|[™®©]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex TrimPlatformName = new Regex(@"\s*(\((?<platform>[^()]+)\)|\[(?<platform>[^\[\]]+)\])$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static Dictionary<string, string[]> GetPlatformSpecsByNormalName(IPlayniteAPI api)
        {
            var platforms = api.Database.Platforms.Where(p => p.SpecificationId != null).ToList();
            var output = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var platform in platforms)
            {
                output.Add(platform.Name, new[] { platform.SpecificationId });
                string nameWithoutCompany = TrimCompanyName.Replace(platform.Name, string.Empty);
                if (!output.ContainsKey(nameWithoutCompany))
                    output.Add(nameWithoutCompany, new[] { platform.SpecificationId });
            }
            TryAddPlatformByName(output, "3DO", "3do");
            TryAddPlatformByName(output, new[] { "Windows", "PC", "PC CD-ROM", "PC DVD", "PC DVD-ROM" }, new[] { "pc_windows" });
            TryAddPlatformByName(output, "DOS", "pc_dos");
            TryAddPlatformByName(output, "Linux", "pc_linux");
            TryAddPlatformByName(output, "Mac", "macintosh");
            TryAddPlatformByName(output, new[] { "Xbox360" }, new[] { "xbox360" });
            TryAddPlatformByName(output, new[] { "XboxOne" }, new[] { "xbox_one" });
            TryAddPlatformByName(output, new[] { "Microsoft Xbox Series X", "Microsoft Xbox Series S", "Xbox Series X", "Xbox Series S", "Microsoft Xbox Series X/S", "Microsoft Xbox Series S/X", "Xbox Series X/S", "Xbox Series S/X", "Xbox Series X|S", "XboxSeriesX" }, new[] { "xbox_series" });
            TryAddPlatformByName(output, new[] { "PS", "PS1", "PSX" }, new[] { "sony_playstation" });
            TryAddPlatformByName(output, "PS2", "sony_playstation2");
            TryAddPlatformByName(output, "PS3", "sony_playstation3");
            TryAddPlatformByName(output, "PS4", "sony_playstation4");
            TryAddPlatformByName(output, "PS5", "sony_playstation5");
            TryAddPlatformByName(output, "PSP", "sony_psp");
            TryAddPlatformByName(output, "Vita", "sony_vita");
            TryAddPlatformByName(output, "PS4/5", new[] { "sony_playstation4", "sony_playstation5" });
            TryAddPlatformByName(output, "Playstation 4/5", new[] { "sony_playstation4", "sony_playstation5" });
            return output;
        }

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

            if (PlatformSpecNameByNormalName.TryGetValue(sanitizedPlatformName, out string[] specIds))
                return specIds.Select(s => new MetadataSpecProperty(s)).ToList<MetadataProperty>();

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
    }
}
