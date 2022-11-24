using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ViveportLibrary
{
    public interface IAppDataReader
    {
        IEnumerable<InstalledAppData> GetInstalledApps();

        IEnumerable<LicensedAppData> GetLicensedApps();
    }

    public class AppDataReader : IAppDataReader
    {
        private static readonly string DefaultAppStatePath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\HTC\Viveport\installed_apps.json");
        private static readonly string DefaultContentMetadataPath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\HTC\Viveport\content_metadata2.pref");
        private ILogger logger = LogManager.GetLogger();

        public string AppStatePath { get; }
        public string ContentMetadataPath { get; }

        public AppDataReader(string appStatePath = null, string contentMetadataPath = null)
        {
            AppStatePath = appStatePath ?? DefaultAppStatePath;
            ContentMetadataPath = contentMetadataPath ?? DefaultContentMetadataPath;
        }

        public IEnumerable<InstalledAppData> GetInstalledApps()
        {
            if (!File.Exists(AppStatePath))
            {
                logger.Error($"Viveport installed games file not found in {AppStatePath}");
                return null;
            }

            var fileContents = File.ReadAllText(AppStatePath);
            var apps = JsonConvert.DeserializeObject<InstalledAppData[]>(fileContents)
                ?.Where(x => x.AppId != null).ToArray();

            return apps;
        }

        public IEnumerable<LicensedAppData> GetLicensedApps()
        {
            if (!File.Exists(ContentMetadataPath))
            {
                logger.Error($"Viveport licensed games file not found in {ContentMetadataPath}");
                return null;
            }

            var fileContents = File.ReadAllText(ContentMetadataPath);
            var items = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContents);
            var output = new List<LicensedAppData>();
            foreach (var kvp in items)
            {
                if (!kvp.Key.EndsWith("_data"))
                    continue;

                var appData = JsonConvert.DeserializeObject<LicensedAppData>(kvp.Value);
                output.Add(appData);
            }

            return output;
        }
    }

    public class InstalledAppData
    {
        [JsonProperty("appId")]
        public string AppId;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("imageUri")]
        public string ImageUri;

        [JsonProperty("path")]
        public string Path;

        [JsonProperty("uri")]
        public string StartupUri;
    }

    public class LicensedAppData
    {
        public string Id;

        [JsonProperty("at")]
        public string Developer;

        [JsonProperty("lc")]
        public string Locale;

        [JsonProperty("sdl")]
        public string[] SupportedDeviceList = new string[0];

        [JsonProperty("srl")]
        public string[] SupportedVrApis = new string[0];

        [JsonProperty("tt")]
        public string Title;

        [JsonProperty("tm")]
        public Dictionary<string, ThumbnailData> Thumbnails = new Dictionary<string, ThumbnailData>();
    }

    public class ThumbnailData
    {
        public string Url;

        [JsonProperty("h")]
        public int Height;

        [JsonProperty("w")]
        public int Width;
    }
}