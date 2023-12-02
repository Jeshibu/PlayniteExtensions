using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteExtensions.Common
{
    public interface IRegistryValueProvider
    {
        string[] GetSubKeysForPath(RegistryView platform, RegistryHive hive, string path);
        string GetValueForPath(RegistryView platform, RegistryHive hive, string path, string keyName);
        string[] GetSubKeysForPath(RegistryHive hive, string path);
        string GetValueForPath(RegistryHive hive, string path, string keyName);
    }

    public class RegistryValueProvider : IRegistryValueProvider
    {
        public RegistryValueProvider() { }

        public string[] GetSubKeysForPath(
            RegistryView platform,
            RegistryHive hive,
            string path)
        {
            RegistryKey rootKey = RegistryKey.OpenBaseKey(hive, platform);

            return rootKey
                    .OpenSubKey(path)
                    ?.GetSubKeyNames();
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

        public string[] GetSubKeysForPath(RegistryHive hive, string path)
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
