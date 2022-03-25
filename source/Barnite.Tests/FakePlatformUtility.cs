using Playnite.SDK.Models;
using System;
using System.Collections.Generic;

namespace Barnite.Tests
{
    public class FakePlatformUtility : IPlatformUtility
    {
        public FakePlatformUtility(string platformName, string specId)
        {
            SpecIds = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) { { platformName, specId } };
        }

        public FakePlatformUtility(Dictionary<string, string> specIds)
        {
            SpecIds = specIds;
        }

        public Dictionary<string, string> SpecIds { get; }

        public MetadataProperty GetPlatform(string platformName)
        {
            if (SpecIds.TryGetValue(platformName, out string specId))
                return new MetadataSpecProperty(specId);

            return new MetadataNameProperty(platformName);
        }
    }
}
