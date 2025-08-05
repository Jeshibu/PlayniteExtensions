using Microsoft.Win32;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;

namespace LegacyGamesLibrary;

public interface ILegacyGamesRegistryReader
{
    IEnumerable<RegistryGameData> GetGameData(RegistryView registryView = RegistryView.Default);
}

public class LegacyGamesRegistryReader(IRegistryValueProvider registryValueProvider) : ILegacyGamesRegistryReader
{
    readonly ILogger logger = LogManager.GetLogger();

    public IEnumerable<RegistryGameData> GetGameData(RegistryView registryView)
    {
        string legacyGamesFolder = @"Software\Legacy Games";
        var gameFolderKeys = registryValueProvider.GetSubKeysForPath(registryView, RegistryHive.CurrentUser, legacyGamesFolder);
        if (gameFolderKeys == null)
        {
            logger.Info($"Failed to get registry subkeys for {legacyGamesFolder}");
            yield break;
        }

        foreach (var gameFolderKey in gameFolderKeys)
        {
            var gameFolder = $@"{legacyGamesFolder}\{gameFolderKey}";
            logger.Info($"Parsing {gameFolder}");

            var gameId = registryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "InstallerUUID");
            if (string.IsNullOrWhiteSpace(gameId))
            {
                logger.Warn($@"No value found for {gameFolder}\InstallerUUID");
                continue;
            }

            yield return new RegistryGameData
            {
                InstallerUUID = new Guid(gameId),
                ProductName = registryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "ProductName"),
                InstDir = registryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "InstDir"),
                GameExe = registryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "GameExe"),
            };
        }
    }

    public string GetLauncherPath()
    {
        string uninstallFolderPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        var uninstallFolderKeys = registryValueProvider.GetSubKeysForPath(RegistryView.Registry64, RegistryHive.LocalMachine, uninstallFolderPath);
        if (uninstallFolderKeys == null)
        {
            logger.Info(@"Failed to get registry subkeys for Software\Microsoft\Windows\CurrentVersion\Uninstall");
            return null;
        }

        foreach (var subKey in uninstallFolderKeys)
        {
            var folder = $@"{uninstallFolderPath}\{subKey}";
            var name = registryValueProvider.GetValueForPath(RegistryView.Registry64, RegistryHive.LocalMachine, folder, "DisplayName");
            if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("Legacy Games Launcher"))
                continue;

            var iconPath = registryValueProvider.GetValueForPath(RegistryView.Registry64, RegistryHive.LocalMachine, folder, "DisplayIcon");
            var dir = System.IO.Path.GetDirectoryName(iconPath);
            return System.IO.Path.Combine(dir, "Legacy Games Launcher.exe");
        }
        return null;
    }
}

public class RegistryGameData
{
    public Guid InstallerUUID { get; set; }
    public string InstDir { get; set; }
    public string GameExe { get; set; }
    public string ProductName { get; set; }
}
