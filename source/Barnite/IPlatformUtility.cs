using Playnite.SDK.Models;
using System.Collections.Generic;

namespace Barnite
{
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
    }
}