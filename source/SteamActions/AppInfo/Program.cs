using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SteamAppInfoParser
{
    class VdfData
    {
        public static List<App> GetAppInfo()
        {
            var steamLocation = GetSteamPath();

            if (steamLocation == null)
                throw new Exception("Steam installation absent or broken");

            var appInfoPath = Path.Combine(steamLocation, "appcache", "appinfo.vdf");
            if (!File.Exists(appInfoPath))
                throw new Exception($"File not found: {appInfoPath}");

            var appInfo = new AppInfo();
            appInfo.Read(appInfoPath);

            return appInfo.Apps;
        }

        private int Main()
        {
            var steamLocation = GetSteamPath();

            if (steamLocation == null)
            {
                Console.Error.WriteLine("Can not find Steam");
                return 1;
            }

            var appInfo = new AppInfo();
            appInfo.Read(Path.Combine(steamLocation, "appcache", "appinfo.vdf"));

            Console.WriteLine($"{appInfo.Apps.Count} apps");

            foreach (var app in appInfo.Apps)
            {
                if (app.Token > 0)
                {
                    Console.WriteLine($"App: {app.AppID} - Token: {app.Token}");
                }
            }

            Console.WriteLine();

            var packageInfo = new PackageInfo();
            packageInfo.Read(Path.Combine(steamLocation, "appcache", "packageinfo.vdf"));

            Console.WriteLine($"{packageInfo.Packages.Count} packages");

            foreach (var package in packageInfo.Packages)
            {
                if (package.Token > 0)
                {
                    Console.WriteLine($"Package: {package.SubID} - Token: {package.Token}");
                }
            }

            return 0;
        }

        private static string GetSteamPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Valve\\Steam") ??
                          RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                              .OpenSubKey("SOFTWARE\\Valve\\Steam");

                if (key != null && key.GetValue("SteamPath") is string steamPath)
                {
                    return steamPath;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var paths = new [] {".steam", ".steam/steam", ".steam/root", ".local/share/Steam"};

                return paths
                    .Select(path => Path.Combine(home, path))
                    .FirstOrDefault(steamPath => Directory.Exists(Path.Combine(steamPath, "appcache")));
            }

            throw new PlatformNotSupportedException();
        }
    }
}
