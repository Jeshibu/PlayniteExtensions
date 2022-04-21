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
        private Dictionary<string, string> platformSpecNameByNormalName;
        private Dictionary<string, string> PlatformSpecNameByNormalName
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
        public PlatformUtility(Dictionary<string,string> overrideValues)
        {
            platformSpecNameByNormalName = overrideValues;
        }

        /// <summary>
        /// For unit testing purposes only
        /// </summary>
        /// <param name="platformName"></param>
        /// <param name="specId"></param>
        public PlatformUtility(string platformName, string specId)
        {
            platformSpecNameByNormalName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) { { platformName, specId } };
        }

        private static Regex TrimCompanyName = new Regex(@"^(atari|bandai|coleco|commodore|mattel|nec|nintendo|sega|sinclair|snk|sony|microsoft)?\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex TrimInput = new Regex(@"^(pal|jpn?|usa?|ntsc)\s+|[™®©]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static Dictionary<string, string> GetPlatformSpecsByNormalName(IPlayniteAPI api)
        {
            var platforms = api.Database.Platforms.Where(p => p.SpecificationId != null).ToList();
            var output = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var platform in platforms)
            {
                output.Add(platform.Name, platform.SpecificationId);
                string nameWithoutCompany = TrimCompanyName.Replace(platform.Name, string.Empty);
                if (!output.ContainsKey(nameWithoutCompany))
                    output.Add(nameWithoutCompany, platform.SpecificationId);
            }
            output.Add("3DO", "3do");
            output.Add("Windows", "pc_windows");
            output.Add("DOS", "pc_dos");
            output.Add("Linux", "pc_linux");
            output.Add("PC CD-ROM", "pc_windows");
            output.Add("PC DVD", "pc_windows");
            output.Add("PC DVD-ROM", "pc_windows");
            output.Add("Microsoft Xbox Series X", "xbox_series");
            output.Add("Microsoft Xbox Series S", "xbox_series");
            output.Add("Xbox Series X", "xbox_series");
            output.Add("Xbox Series S", "xbox_series");
            output.Add("Microsoft Xbox Series X/S", "xbox_series");
            output.Add("Microsoft Xbox Series S/X", "xbox_series");
            output.Add("Xbox Series X/S", "xbox_series");
            output.Add("Xbox Series S/X", "xbox_series");
            output.Add("PS", "sony_playstation");
            output.Add("PSX", "sony_playstation");
            output.Add("PS1", "sony_playstation");
            output.Add("PS2", "sony_playstation2");
            output.Add("PS3", "sony_playstation3");
            output.Add("PS4", "sony_playstation4");
            output.Add("PS5", "sony_playstation5");
            output.Add("PSP", "sony_psp");
            output.Add("Vita", "sony_vita");
            return output;
        }

        public MetadataProperty GetPlatform(string platformName)
        {
            string sanitizedPlatformName = TrimInput.Replace(platformName, string.Empty);

            if (PlatformSpecNameByNormalName.TryGetValue(sanitizedPlatformName, out string specId))
                return new MetadataSpecProperty(specId);

            return new MetadataNameProperty(sanitizedPlatformName);
        }

        public IEnumerable<string> GetPlatformNames()
        {
            return PlatformSpecNameByNormalName.Keys;
        }
    }
}
