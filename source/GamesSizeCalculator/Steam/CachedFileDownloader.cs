using PluginsCommon;
using PluginsCommon.Web;
using System;
using System.IO;
using System.Text;

namespace GamesSizeCalculator.SteamSizeCalculation;

public interface ICachedFile
{
    string GetFileContents();
    void RefreshCache();
}

public class CachedFileDownloader(string onlinePath, string localPath, TimeSpan maxCacheAge, Encoding encoding = null, string packagedFallbackPath = null) : ICachedFile
{
    public string OnlinePath { get; } = onlinePath;
    public string LocalPath { get; } = Environment.ExpandEnvironmentVariables(localPath);
    public TimeSpan MaxCacheAge { get; } = maxCacheAge;
    public Encoding Encoding { get; } = encoding;
    public string PackagedFallbackPath { get; } = packagedFallbackPath;

    private bool CopyFileFromPackagedFallback()
    {
        if (PackagedFallbackPath.IsNullOrWhiteSpace())
        {
            return false;
        }

        FileInfo packagedFallbackFile = new(PackagedFallbackPath);

        if (!packagedFallbackFile.Exists)
        {
            return false;
        }

        FileSystem.CopyFile(PackagedFallbackPath, LocalPath, true);
        return true;
    }

    private bool PackagedFallbackIsNewerThan(FileInfo f)
    {
        if (PackagedFallbackPath.IsNullOrWhiteSpace())
        {
            return false;
        }

        FileInfo packagedFallbackFile = new(PackagedFallbackPath);
        if (!packagedFallbackFile.Exists || !f.Exists)
        {
            return false;
        }

        return packagedFallbackFile.LastWriteTime > f.LastWriteTime;
    }

    public string GetFileContents()
    {
        var f = new FileInfo(LocalPath);
        if ((!f.Exists || PackagedFallbackIsNewerThan(f)) && CopyFileFromPackagedFallback())
        {
            f.Refresh();
        }

        if (!f.Exists || f.LastWriteTime + MaxCacheAge < DateTime.Now)
        {
            RefreshCache();
        }
        if (Encoding == null)
        {
            return FileSystem.ReadStringFromFile(LocalPath);
        }
        else
        {
            return File.ReadAllText(LocalPath, Encoding);
        }
    }

    public void RefreshCache()
    {
        HttpDownloader.DownloadFile(OnlinePath, LocalPath);
    }
}