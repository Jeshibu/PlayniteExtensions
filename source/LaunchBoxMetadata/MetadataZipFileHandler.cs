using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace LaunchBoxMetadata
{
    public class MetadataZipFileHandler : IDisposable
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlayniteAPI playniteAPI;
        private readonly LaunchBoxMetadataSettings settings;
        private readonly string oldEtag;
        private readonly DateTimeOffset? oldLastModified;
        private readonly List<string> tempPaths = new List<string>();

        public MetadataZipFileHandler(IPlayniteAPI playniteAPI, LaunchBoxMetadataSettings settings)
        {
            this.playniteAPI = playniteAPI;
            this.settings = settings;
            oldEtag = settings?.MetadataZipEtag;
            oldLastModified = settings?.MetadataZipLastModified;
        }

        public void Dispose()
        {
            try
            {
                foreach (var path in tempPaths)
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during dispose");
            }
        }

        public string DownloadMetadataZipFile(string url = "https://gamesdb.launchbox-app.com/Metadata.zip")
        {
            var zipPath = Path.GetTempFileName() + ".zip";

            playniteAPI.Dialogs.ActivateGlobalProgress(async a =>
            {
                byte[] buffer = new byte[1024 * 10];
                var bytesDownloaded = 0;
                try
                {
                    using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromHours(1) })
                    using (var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), HttpCompletionOption.ResponseHeadersRead, a.CancelToken))
                    {
                        var etag = response.Headers.ETag?.Tag;
                        var lastModified = response.Content.Headers.LastModified;
                        var contentLength = response.Content.Headers.ContentLength;

                        if (etag != null && oldEtag == etag)
                        {
                            string lastUpdatedString = lastModified.HasValue ? $" (last updated {lastModified.Value:g})" : "";
                            var dialogResult = playniteAPI.Dialogs.ShowMessage($"Downloadable LaunchBox metadata{lastUpdatedString} has not changed since you last updated your local LaunchBox metadata database. Update anyway?", "Force update?", System.Windows.MessageBoxButton.YesNo);
                            if (dialogResult != System.Windows.MessageBoxResult.Yes)
                            {
                                return;
                            }
                        }

                        if (contentLength.HasValue)
                        {
                            a.IsIndeterminate = false;
                            a.ProgressMaxValue = contentLength.Value;
                        }

                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        using (var targetFile = File.Create(zipPath))
                        {
                            while (true)
                            {
                                if (a.CancelToken.IsCancellationRequested)
                                {
                                    targetFile.Dispose();
                                    File.Delete(zipPath);
                                    return;
                                }

                                int bufferContentLength = responseStream.Read(buffer, 0, buffer.Length);
                                if (bufferContentLength == 0)
                                {
                                    targetFile.Flush();
                                    tempPaths.Add(zipPath);
                                    settings.MetadataZipEtag = etag;
                                    settings.MetadataZipLastModified = lastModified;
                                    return;
                                }

                                targetFile.Write(buffer, 0, bufferContentLength);

                                bytesDownloaded += bufferContentLength;
                                a.CurrentProgressValue = bytesDownloaded;
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error downloading metadata zip. Deleting {zipPath}");
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);
                }
            }, new GlobalProgressOptions("Downloading LaunchBox metadata...", cancelable: true));

            if (File.Exists(zipPath))
                return zipPath;
            else
                return null;
        }

        public string ExtractMetadataXmlFromZipFile(string zipFilePath)
        {
            var xmlPath = Path.GetTempFileName() + ".xml";
            var cleanedXmlPath = Path.GetTempFileName() + ".xml";
            tempPaths.Add(xmlPath);
            tempPaths.Add(cleanedXmlPath);

            playniteAPI.Dialogs.ActivateGlobalProgress(a =>
            {
                var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Read);
                var entry = zip.GetEntry("Metadata.xml");
                entry.ExtractToFile(xmlPath, overwrite: true);

                a.Text = "Cleaning up file...";

                PurgeControlCharacterEntities(xmlPath, cleanedXmlPath);
            }, new GlobalProgressOptions("Extracting zip file..."));

            return cleanedXmlPath;
        }

        private static void PurgeControlCharacterEntities(string inputFilePath, string outputFilePath)
        {
            var hexEntityRegex = new Regex(@"&#x[0-9A-F]{1,2};", RegexOptions.Compiled);

            using (var writer = new StreamWriter(outputFilePath))
            {
                foreach (var line in File.ReadLines(inputFilePath))
                {
                    var cleanLine = hexEntityRegex.Replace(line, string.Empty);
                    writer.WriteLine(cleanLine);
                }
            }
        }
    }
}
