using Microsoft.Win32;
using PlayniteExtensions.Common;

namespace BigFishLibrary;

public class BigFishRegistryReader(IRegistryValueProvider registryValueProvider)
{
    private const string baseRegistryPath = @"SOFTWARE\WOW6432Node\Big Fish Games";

    public string GetClientInstallDirectory()
    {
        return registryValueProvider.GetValueForPath(RegistryHive.LocalMachine, $@"{baseRegistryPath}\Client", "InstallationPath");
    }

    public string[] GetInstalledGameIds()
    {
        return registryValueProvider.GetSubKeysForPath(RegistryHive.LocalMachine, $@"{baseRegistryPath}\Persistence\GameDB");
    }

    public BigFishGameDetails GetGameDetails(string id)
    {
        var output = new BigFishGameDetails { Sku = id };
        var path = $@"{baseRegistryPath}\Persistence\GameDB\{id}";
        output.ExecutablePath = registryValueProvider.GetValueForPath(RegistryHive.LocalMachine, path, "ExecutablePath");
        output.Thumbnail = registryValueProvider.GetValueForPath(RegistryHive.LocalMachine, path, "Thumbnail");
        output.Name = registryValueProvider.GetValueForPath(RegistryHive.LocalMachine, path, "Name");
        return output;
    }
}

public class BigFishGameDetails
{
    public string Sku { get; set; }
    public string ExecutablePath { get; set; }
    public string Thumbnail { get; set; }
    public string Name { get; set; }
}
