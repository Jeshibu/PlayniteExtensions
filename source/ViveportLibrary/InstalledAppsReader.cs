using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViveportLibrary
{

    public interface IInstalledAppsReader
    {
        IEnumerable<InstalledAppData> GetInstalledApps();
    }

    public class InstalledAppsReader : IInstalledAppsReader
    {
        private static readonly string DefaultAppStatePath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\HTC\Viveport\installed_apps.json");
        private ILogger logger = LogManager.GetLogger();

        public string AppStatePath { get; }

        public InstalledAppsReader(string appStatePath = null)
        {
            AppStatePath = appStatePath ?? DefaultAppStatePath;
        }

        public IEnumerable<InstalledAppData> GetInstalledApps()
        {
            if (!File.Exists(AppStatePath))
            {
                logger.Info($"Viveport installed games file not found in {AppStatePath}");
                return null;
            }

            var fileContents = File.ReadAllText(AppStatePath);
            var apps = JsonConvert.DeserializeObject<InstalledAppData[]>(fileContents);

            return apps;
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
}
