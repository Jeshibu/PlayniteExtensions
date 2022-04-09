using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

namespace PlayniteExtensions.Common
{
    public interface ICachedFile
    {
        string GetFileContents();
        void RefreshCache();
    }

    public class CachedFileDownloader : ICachedFile
    {
        public CachedFileDownloader(string onlinePath, string localPath, TimeSpan maxCacheAge, Encoding encoding = null, string packagedFallbackPath = null)
        {
            OnlinePath = onlinePath;
            LocalPath = Environment.ExpandEnvironmentVariables(localPath);
            MaxCacheAge = maxCacheAge;
            Encoding = encoding;
            PackagedFallbackPath = packagedFallbackPath;
        }

        public string OnlinePath { get; }
        public string LocalPath { get; }
        public TimeSpan MaxCacheAge { get; }
        public Encoding Encoding { get; }
        public string PackagedFallbackPath { get; }

        private bool CopyFileFromPackagedFallback()
        {
            FileInfo packagedFallbackFile = new FileInfo(PackagedFallbackPath);

            if (string.IsNullOrEmpty(PackagedFallbackPath) || !packagedFallbackFile.Exists)
                return false;

            File.Copy(PackagedFallbackPath, LocalPath, overwrite: true);
            File.SetCreationTime(LocalPath, packagedFallbackFile.CreationTime);
            File.SetLastWriteTime(LocalPath, packagedFallbackFile.CreationTime);
            return true;
        }

        public string GetFileContents()
        {
            var f = new FileInfo(LocalPath);

            if (!f.Exists && CopyFileFromPackagedFallback())
            {
                f.Refresh();
            }

            if (!f.Exists || f.CreationTime + MaxCacheAge < DateTime.Now)
            {
                RefreshCache();
            }
            if (Encoding == null)
                return File.ReadAllText(LocalPath);
            else
                return File.ReadAllText(LocalPath, Encoding);
        }

        public void RefreshCache()
        {
            using (var w = new WebClient())
            {
                w.DownloadFile(OnlinePath, LocalPath);
            }
        }
    }
}
