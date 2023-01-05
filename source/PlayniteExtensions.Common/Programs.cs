using Microsoft.Win32;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PlayniteExtensions.Common
{
    public class UninstallProgram
    {
        public string DisplayIcon { get; set; }
        public string DisplayName { get; set; }
        public string DisplayVersion { get; set; }
        public string InstallLocation { get; set; }
        public string Publisher { get; set; }
        public string UninstallString { get; set; }
        public string URLInfoAbout { get; set; }
        public string RegistryKeyName { get; set; }
        public string Path { get; set; }

        public override string ToString()
        {
            return DisplayName ?? RegistryKeyName;
        }
    }

    public static class Programs
    {
        private static readonly string[] scanFileExclusionMasks = new string[]
        {
            "uninst",
            "setup",
            @"unins\d+",
            "Config",
            "DXSETUP",
            @"vc_redist\.x64",
            @"vc_redist\.x86",
            @"^UnityCrashHandler32\.exe$",
            @"^UnityCrashHandler64\.exe$",
            @"^notification_helper\.exe$",
            @"^python\.exe$",
            @"^pythonw\.exe$",
            @"^zsync\.exe$",
            @"^zsyncmake\.exe$"
        };

        private static ILogger logger = LogManager.GetLogger();

        public static bool IsFileScanExcluded(string path)
        {
            return scanFileExclusionMasks.Any(a => Regex.IsMatch(path, a, RegexOptions.IgnoreCase));
        }

        private static IEnumerable<UninstallProgram> SearchRoot(RegistryHive hive, RegistryView view)
        {
            const string rootString = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            {
                var keyList = root.OpenSubKey(rootString);
                if (keyList == null)
                {
                    yield break;
                }

                foreach (var key in keyList.GetSubKeyNames())
                {
                    UninstallProgram program = null;
                    try
                    {
                        using (var prog = root.OpenSubKey(rootString + key))
                        {
                            if (prog == null)
                            {
                                continue;
                            }

                            program = new UninstallProgram()
                            {
                                DisplayIcon = prog.GetValue("DisplayIcon")?.ToString(),
                                DisplayVersion = prog.GetValue("DisplayVersion")?.ToString(),
                                DisplayName = prog.GetValue("DisplayName")?.ToString(),
                                InstallLocation = prog.GetValue("InstallLocation")?.ToString(),
                                Publisher = prog.GetValue("Publisher")?.ToString(),
                                UninstallString = prog.GetValue("UninstallString")?.ToString(),
                                URLInfoAbout = prog.GetValue("URLInfoAbout")?.ToString(),
                                Path = prog.GetValue("Path")?.ToString(),
                                RegistryKeyName = key
                            };
                        }
                    }
                    catch (System.Security.SecurityException e)
                    {
                        logger.Warn(e, $"Failed to read registry key {rootString + key}");
                    }

                    if (program != null)
                        yield return program;
                }
            }
        }


        public static IEnumerable<UninstallProgram> GetUninstallPrograms()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                foreach (var p in SearchRoot(RegistryHive.LocalMachine, RegistryView.Registry64))
                    yield return p;

                foreach (var p in SearchRoot(RegistryHive.CurrentUser, RegistryView.Registry64))
                    yield return p;
            }

            foreach (var p in SearchRoot(RegistryHive.LocalMachine, RegistryView.Registry32))
                yield return p;

            foreach (var p in SearchRoot(RegistryHive.CurrentUser, RegistryView.Registry32))
                yield return p;
        }

        public static UninstallProgram GetUninstallProgram(string displayName)
        {
            return GetUninstallPrograms().FirstOrDefault(p => p.DisplayName == displayName);
        }
    }
}
