using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Barnite
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
            output.Add("3DO", new[] { "3do" });
            output.Add("Windows", new[] { "pc_windows" });
            output.Add("DOS", new[] { "pc_dos" });
            output.Add("Linux", new[] { "pc_linux" });
            output.Add("PC CD-ROM", new[] { "pc_windows" });
            output.Add("PC DVD", new[] { "pc_windows" });
            output.Add("PC DVD-ROM", new[] { "pc_windows" });
            output.Add("Microsoft Xbox Series X", new[] { "xbox_series" });
            output.Add("Microsoft Xbox Series S", new[] { "xbox_series" });
            output.Add("Xbox Series X", new[] { "xbox_series" });
            output.Add("Xbox Series S", new[] { "xbox_series" });
            output.Add("Microsoft Xbox Series X/S", new[] { "xbox_series" });
            output.Add("Microsoft Xbox Series S/X", new[] { "xbox_series" });
            output.Add("Xbox Series X/S", new[] { "xbox_series" });
            output.Add("Xbox Series S/X", new[] { "xbox_series" });
            output.Add("PS", new[] { "sony_playstation" });
            output.Add("PSX", new[] { "sony_playstation" });
            output.Add("PS1", new[] { "sony_playstation" });
            output.Add("PS2", new[] { "sony_playstation2" });
            output.Add("PS3", new[] { "sony_playstation3" });
            output.Add("PS4", new[] { "sony_playstation4" });
            output.Add("PS5", new[] { "sony_playstation5" });
            output.Add("PSP", new[] { "sony_psp" });
            output.Add("Vita", new[] { "sony_vita" });
            output.Add("PS4/5", new[] { "sony_playstation4", "sony_playstation5" });
            output.Add("Playstation 4/5", new[] { "sony_playstation4", "sony_playstation5" });
            return output;
        }

        public IEnumerable<MetadataProperty> GetPlatforms(string platformName)
        {
            return GetPlatforms(platformName, strict: false);
        }

        public IEnumerable<MetadataProperty> GetPlatforms(string platformName, bool strict)
        {
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
    }
}
