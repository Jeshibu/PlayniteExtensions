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

        private static Regex TrimCompanyName = new Regex(@"^(atari|bandai|coleco|commodore|mattel|nec|nintendo|sega|sinclair|snk|sony|microsoft)\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex TrimRegion = new Regex(@"^(pal|jpn?|usa?|ntsc)\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

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
            output.Add("Vita", "sony_vita");
            return output;
        }

        public MetadataProperty GetPlatform(string platformName)
        {
            string regionlessPlatformName = TrimRegion.Replace(platformName, string.Empty);

            if (PlatformSpecNameByNormalName.TryGetValue(regionlessPlatformName, out string specId))
                return new MetadataSpecProperty(specId);

            return new MetadataNameProperty(regionlessPlatformName);
        }
    }
}
