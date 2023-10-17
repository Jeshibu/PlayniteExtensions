using Microsoft.Win32;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LegacyGamesLibrary
{
    public interface ILegacyGamesRegistryReader
    {
        IEnumerable<RegistryGameData> GetGameData(RegistryView registryView = RegistryView.Default);
    }

    public class LegacyGamesRegistryReader : ILegacyGamesRegistryReader
    {
        ILogger logger = LogManager.GetLogger();

        public LegacyGamesRegistryReader(IRegistryValueProvider registryValueProvider)
        {
            RegistryValueProvider = registryValueProvider;
        }

        private IRegistryValueProvider RegistryValueProvider { get; }

        public IEnumerable<RegistryGameData> GetGameData(RegistryView registryView)
        {
            string legacyGamesFolder = @"Software\Legacy Games";
            var gameFolderKeys = RegistryValueProvider.GetSubKeysForPath(registryView, RegistryHive.CurrentUser, legacyGamesFolder);
            if (gameFolderKeys == null)
            {
                logger.Info($"Failed to get registry subkeys for {legacyGamesFolder}");
                yield break;
            }

            foreach (var gameFolderKey in gameFolderKeys)
            {
                var gameFolder = $@"{legacyGamesFolder}\{gameFolderKey}";
                yield return new RegistryGameData
                {
                    InstallerUUID = new Guid(RegistryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "InstallerUUID")),
                    ProductName = RegistryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "ProductName"),
                    InstDir = RegistryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "InstDir"),
                    GameExe = RegistryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "GameExe"),
                };
            }
        }

        public string GetLauncherPath()
        {
            string uninstallFolderPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            var uninstallFolderKeys = RegistryValueProvider.GetSubKeysForPath(RegistryView.Registry64, RegistryHive.LocalMachine, uninstallFolderPath);
            if (uninstallFolderKeys == null)
            {
                logger.Info(@"Failed to get registry subkeys for Software\Microsoft\Windows\CurrentVersion\Uninstall");
                return null;
            }

            foreach (var subKey in uninstallFolderKeys)
            {
                var folder = $@"{uninstallFolderPath}\{subKey}";
                var name = RegistryValueProvider.GetValueForPath(RegistryView.Registry64, RegistryHive.LocalMachine, folder, "DisplayName");
                if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("Legacy Games Launcher"))
                    continue;

                var iconPath = RegistryValueProvider.GetValueForPath(RegistryView.Registry64, RegistryHive.LocalMachine, folder, "DisplayIcon");
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

    public interface IRegistryValueProvider
    {
        List<string> GetSubKeysForPath(RegistryView platform, RegistryHive hive, string path);
        string GetValueForPath(RegistryView platform, RegistryHive hive, string path, string keyName);
        List<string> GetSubKeysForPath(RegistryHive hive, string path);
        string GetValueForPath(RegistryHive hive, string path, string keyName);
    }

    public class RegistryValueProvider : IRegistryValueProvider
    {
        public RegistryValueProvider() { }

        public List<string> GetSubKeysForPath(
            RegistryView platform,
            RegistryHive hive,
            string path)
        {
            RegistryKey rootKey = RegistryKey.OpenBaseKey(hive, platform);

            return rootKey
                    .OpenSubKey(path)
                    ?.GetSubKeyNames()
                    ?.ToList();
        }

        public string GetValueForPath(
            RegistryView platform,
            RegistryHive hive,
            string path,
            string keyName)
        {
            RegistryKey rootKey = RegistryKey.OpenBaseKey(hive, platform);

            return rootKey
                        .OpenSubKey(path)
                        ?.GetValue(keyName)
                        ?.ToString();
        }

        public List<string> GetSubKeysForPath(RegistryHive hive, string path)
        {
            return GetSubKeysForPath(RegistryView.Registry64, hive, path)
                ?? GetSubKeysForPath(RegistryView.Registry32, hive, path);
        }

        public string GetValueForPath(RegistryHive hive, string path, string keyName)
        {
            return GetValueForPath(RegistryView.Registry64, hive, path, keyName)
                ?? GetValueForPath(RegistryView.Registry32, hive, path, keyName);
        }

    }
}
