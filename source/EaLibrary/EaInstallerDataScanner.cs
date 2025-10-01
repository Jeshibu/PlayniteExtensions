using EaLibrary.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace EaLibrary;

public class EaInstallerDataScanner
{
    private readonly ILogger _logger = LogManager.GetLogger();

    public List<InstallerData> GetManifests(CancellationToken cancellationToken = default)
    {
        var driveTypes = new[] { DriveType.Fixed, DriveType.Removable };
        var drives = DriveInfo.GetDrives();
        var manifests = new List<InstallerData>();
        foreach (var driveInfo in drives.Where(d => driveTypes.Contains(d.DriveType)))
            GetGamesFromDirectory(manifests, driveInfo.RootDirectory, cancellationToken, 0);

        return manifests;
    }

    public List<InstallerData> GetManifests(DirectoryInfo dir, CancellationToken cancellationToken = default)
    {
        int depth = dir.Parent == null ? 0 : 1;
        return GetManifests(dir, cancellationToken, depth);
    }

    private List<InstallerData> GetManifests(DirectoryInfo dir, CancellationToken cancellationToken, int depth)
    {
        var manifests = new List<InstallerData>();
        GetGamesFromDirectory(manifests, dir, cancellationToken, depth);
        return manifests;
    }

    private void GetGamesFromDirectory(List<InstallerData> manifests, DirectoryInfo directory, CancellationToken cancellationToken, int depth = 0)
    {
        foreach (var subDirectory in directory.GetDirectories())
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (depth == 0 && SkipRootDirectory(subDirectory.Name))
                continue;

            if (HasInstallerData(subDirectory, out var installerData))
            {
                manifests.Add(installerData);
            }
            else
            {
                GetGamesFromDirectory(manifests, subDirectory, cancellationToken, depth: depth + 1);
            }
        }
    }

    private bool HasInstallerData(DirectoryInfo directory, out InstallerData installerData)
    {
        installerData = null;
        if (directory.Name != "__Installer")
            return false;

        var installerDataFile = new FileInfo(Path.Combine(directory.FullName, "installerdata.xml"));
        if (!installerDataFile.Exists)
            return false;

        installerData = GetDataFromXml(installerDataFile);
        return true;
    }

    private InstallerData GetDataFromXml(FileInfo fileInfo)
    {
        var doc = new XmlDocument();
        doc.Load(fileInfo.FullName);
        var xPathNavigator = doc.CreateNavigator();
        var name = GetFirstElementContent(xPathNavigator, "//localeInfo[@locale=\"en_US\"]/title")
                   ?? GetFirstElementContent(xPathNavigator, "//gameTitles/gameTitle[@locale=\"en_US\"]");
        var uninstall = GetFirstElementContent(xPathNavigator, "//uninstall/path");
        
        return new()
        {
            Name = name,
            InstallDirectory = fileInfo.Directory!.Parent!.FullName,
            UninstallerPath = uninstall
        };
    }

    private string GetFirstElementContent(XPathNavigator xPathNavigator, string xpath) => GetElementContents(xPathNavigator, xpath).FirstOrDefault();

    private IEnumerable<string> GetElementContents(XPathNavigator xPathNavigator, string xpath)
    {
        var iterator = xPathNavigator!.Select(xpath);
        while (iterator.MoveNext())
            if (!string.IsNullOrWhiteSpace(iterator.Current.Value))
                yield return iterator.Current.Value;
    }

    private static readonly string[] skipRootDirectories = ["users", "windows", "temp", "tmp"];
    private static bool SkipRootDirectory(string directory) => skipRootDirectories.Contains(directory, StringComparer.OrdinalIgnoreCase);
}

public class InstallerData
{
    public string Name { get; set; }
    public string InstallDirectory { get; set; }
    public string UninstallerPath { get; set; }
}