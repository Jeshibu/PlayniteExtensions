using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ViveportLibrary;

public interface IAppDataReader
{
    IEnumerable<InstalledAppData> GetInstalledApps();

    IEnumerable<AppMetadata> GetAppMetadata();

    IEnumerable<LicenseData> GetLicenseData();
}

public class AppDataReader : IAppDataReader
{
    private static readonly string DefaultAppStatePath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\HTC\Viveport\installed_apps.json");
    private static readonly string DefaultContentMetadataPath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\HTC\Viveport\content_metadata2.pref");
    private static readonly string DefaultLicenseDataPath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\HTC\Viveport\content_licensing2.pref");
    private ILogger logger = LogManager.GetLogger();

    public string AppStatePath { get; }
    public string ContentMetadataPath { get; }
    public string LicenseDataPath { get; }

    public AppDataReader(string appStatePath = null, string contentMetadataPath = null, string licenseDataPath = null)
    {
        AppStatePath = appStatePath ?? DefaultAppStatePath;
        ContentMetadataPath = contentMetadataPath ?? DefaultContentMetadataPath;
        LicenseDataPath = licenseDataPath ?? DefaultLicenseDataPath;
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

    public IEnumerable<AppMetadata> GetAppMetadata()
    {
        if (!File.Exists(ContentMetadataPath))
        {
            logger.Error($"Viveport metadata file not found in {ContentMetadataPath}");
            return null;
        }

        var fileContents = File.ReadAllText(ContentMetadataPath);
        var items = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContents);
        var output = new List<AppMetadata>();
        foreach (var kvp in items)
        {
            if (!kvp.Key.EndsWith("_data"))
                continue;

            var appData = JsonConvert.DeserializeObject<AppMetadata>(kvp.Value);
            if (appData.Id != null)
                output.Add(appData);
        }

        return output;
    }

    public IEnumerable<LicenseData> GetLicenseData()
    {
        if (!File.Exists(LicenseDataPath))
        {
            logger.Error($"Viveport licensed games file not found in {LicenseDataPath}");
            return null;
        }

        var fileContents = File.ReadAllText(LicenseDataPath);
        var items = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContents);
        var output = new List<LicenseData>();
        foreach (var kvp in items)
        {
            if (!kvp.Key.EndsWith("_data"))
                continue;

            var licenseData = JsonConvert.DeserializeObject<LicenseData[]>(kvp.Value);
            output.AddRange(licenseData.Where(l => l.AppId != null));
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

public class AppMetadata
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
    public Dictionary<string, ThumbnailData> Thumbnails = [];
}

public class ThumbnailData
{
    public string Url;

    [JsonProperty("h")]
    public int Height;

    [JsonProperty("w")]
    public int Width;
}

public class LicenseData
{
    public string AppId;
    public ulong? CreatedTime;
    public ulong? ExpiryTime;
    public ulong? OwnershipEndTime;
    public string Licensing;

    public bool IsSubscription => Licensing == "rsu";
}