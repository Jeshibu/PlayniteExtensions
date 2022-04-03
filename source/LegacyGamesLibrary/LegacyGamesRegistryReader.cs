using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyGamesLibrary
{
    public interface ILegacyGamesRegistryReader
    {
        IEnumerable<RegistryGameData> GetGameData(RegistryView registryView = RegistryView.Default);
    }

    public class LegacyGamesRegistryReader : ILegacyGamesRegistryReader
    {
        public LegacyGamesRegistryReader(IRegistryValueProvider registryValueProvider)
        {
            RegistryValueProvider = registryValueProvider;
        }

        private IRegistryValueProvider RegistryValueProvider { get; }

        public IEnumerable<RegistryGameData> GetGameData(RegistryView registryView)
        {
            string legacyGamesFolder = @"Software\Legacy Games";
            var gameFolderKeys = RegistryValueProvider.GetSubKeysForPath(registryView, RegistryHive.CurrentUser, legacyGamesFolder);
            foreach (var gameFolderKey in gameFolderKeys)
            {
                var gameFolder = legacyGamesFolder + "\\" + gameFolderKey;
                yield return new RegistryGameData
                {
                    InstallerUUID = new Guid(RegistryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "InstallerUUID")),
                    ProductName = RegistryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "ProductName"),
                    InstDir = RegistryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "InstDir"),
                    GameExe = RegistryValueProvider.GetValueForPath(registryView, RegistryHive.CurrentUser, gameFolder, "GameExe"),
                };
            }
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
                    .GetSubKeyNames()
                    .ToList();
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
                        .GetValue(keyName)
                        .ToString();
        }
    }
}
