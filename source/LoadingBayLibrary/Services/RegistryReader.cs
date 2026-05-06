using Microsoft.Win32;
using PlayniteExtensions.Common;
using System.Collections.Generic;

namespace LoadingBayLibrary.Services;

public class RegistryReader(IRegistryValueProvider registryValueProvider)
{
    private const string baseRegistryPath = @"SOFTWARE\WOW6432Node\Big Fish Games";

    public string GetClientPath()
    {
        return registryValueProvider.GetValueForPath(RegistryHive.ClassesRoot, "LoadingBay", "URL Protocol");
    }

    public IEnumerable<InstalledGameData> GetInstalledGames()
    {
        const string basePath = @"Software\LoadingBay\LoadingBayInstaller\game";
        var ids = registryValueProvider.GetSubKeysForPath(RegistryHive.CurrentUser, basePath);
        foreach (var id in ids)
        {
            var installPath = registryValueProvider.GetValueForPath(RegistryHive.CurrentUser, $@"{basePath}\{id}", "InstallPath");
            yield return new() { Id = id, InstallPath = installPath };
        }
    }
}

public class InstalledGameData
{
    public string Id;
    public string InstallPath;
};
