using Playnite.SDK.Models;
using System.Collections.Generic;

namespace PlayniteExtensions.Common;

public interface IPlatformUtility
{
    IEnumerable<MetadataProperty> GetPlatforms(string platformName);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="platformName"></param>
    /// <param name="strict">Only return matched platforms</param>
    /// <returns></returns>
    IEnumerable<MetadataProperty> GetPlatforms(string platformName, bool strict);
    IEnumerable<string> GetPlatformNames();

    /// <summary>
    /// Get rid of and output platforms like "Cities in Motion (Mac)" or "Mad Max [PC]"
    /// </summary>
    /// <param name="name">A game name</param>
    /// <param name="trimmedName">The game name with the platform name removed</param>
    /// <returns></returns>
    IEnumerable<MetadataProperty> GetPlatformsFromName(string name, out string trimmedName);

    bool PlatformsOverlap(List<Platform> platforms, List<MetadataProperty> metadataPlatforms, bool returnValueWhenEmpty = true);
    bool PlatformsOverlap(List<Platform> platforms, IEnumerable<string> metadataPlatforms, bool returnValueWhenEmpty = true);
}