using Playnite.SDK.Models;
using System.Collections.Generic;

namespace Barnite
{
    public interface IPlatformUtility
    {
        MetadataProperty GetPlatform(string platformName);
        IEnumerable<string> GetPlatformNames();
    }
}