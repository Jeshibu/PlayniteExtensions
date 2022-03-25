using Playnite.SDK.Models;

namespace Barnite
{
    public interface IPlatformUtility
    {
        MetadataProperty GetPlatform(string platformName);
    }
}