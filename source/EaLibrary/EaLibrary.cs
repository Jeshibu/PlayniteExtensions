﻿using EaLibrary.Models;
using Microsoft.Win32;
using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace EaLibrary;

[LoadPlugin]
public class EaLibrary : LibraryPluginBase<EaLibrarySettingsViewModel>
{
    private static readonly ILogger logger = LogManager.GetLogger();
    private readonly string installManifestCacheDir;
    private static readonly string fakeOfferType = "none";
    private IWebDownloader _downloader;
    public IWebDownloader Downloader => _downloader ??= new WebDownloader();

    public class InstallPackage
    {
        public string OriginalId { get; set; }
        public string ConvertedId { get; set; }
        public string Source { get; set; }
    }

    public class PlatformPath
    {
        public string CompletePath { get; set; }
        public string Root { get; set; }
        public string Path { get; set; }

        public PlatformPath(string completePath)
        {
            CompletePath = completePath;
        }

        public PlatformPath(string root, string path)
        {
            Root = root;
            Path = path;
            CompletePath = System.IO.Path.Combine(root, path);
        }
    }

    public EaLibrary(IPlayniteAPI api) : base(
        "EA app",
        Guid.Parse("85DD7072-2F20-4E76-A007-41035E390724"),
        new LibraryPluginProperties { CanShutdownClient = true, HasSettings = true },
        new EaClient(),
        EaApp.Icon,
        _ => new EaLibrarySettingsView(),
        api)
    {
        SettingsViewModel = new EaLibrarySettingsViewModel(this, PlayniteApi);
        installManifestCacheDir = Path.Combine(GetPluginUserDataPath(), "installmanifests");
    }
    
    internal PlatformPath GetPathFromPlatformPath(string path, RegistryView platformView)
    {
        if (!path.StartsWith("["))
        {
            return new PlatformPath(path);
        }

        var matchPath = Regex.Match(path, @"\[(.*?)\\(.*)\\(.*)\](.*)");
        if (!matchPath.Success)
        {
            Logger.Warn("Unknown path format " + path);
            return null;
        }

        var root = matchPath.Groups[1].Value;
        RegistryKey rootKey = null;

        switch (root)
        {
            case "HKEY_LOCAL_MACHINE":
                rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, platformView);
                break;

            case "HKEY_CURRENT_USER":
                rootKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, platformView);
                break;

            default:
                throw new Exception("Unknown registry root entry " + root);
        }

        var subPath = matchPath.Groups[2].Value.Trim(Path.DirectorySeparatorChar);
        var key = matchPath.Groups[3].Value;
        var executable = matchPath.Groups[4].Value.Trim(Path.DirectorySeparatorChar);
        var subKey = rootKey.OpenSubKey(subPath);
        if (subKey == null)
        {
            return null;
        }

        var keyValue = rootKey.OpenSubKey(subPath).GetValue(key);
        if (keyValue == null)
        {
            return null;
        }

        return new PlatformPath(keyValue.ToString(), executable);
    }

    internal PlatformPath GetPathFromPlatformPath(string path)
    {
        var resultPath = GetPathFromPlatformPath(path, RegistryView.Registry64);
        if (resultPath == null)
        {
            resultPath = GetPathFromPlatformPath(path, RegistryView.Registry32);
        }

        return resultPath;
    }

    private NameValueCollection ParseOriginManifest(string path)
    {
        var text = File.ReadAllText(path);
        var data = HttpUtility.UrlDecode(text);
        return HttpUtility.ParseQueryString(data);
    }

    internal GameInstallerData GetGameInstallerData(string dataPath)
    {
        try
        {
            if (File.Exists(dataPath))
            {
                var ser = new XmlSerializer(typeof(GameInstallerData));
                return (GameInstallerData)ser.Deserialize(XmlReader.Create(dataPath));
            }
            else
            {
                var rootDir = dataPath;
                for (int i = 0; i < 4; i++)
                {
                    var target = Path.Combine(rootDir, "__Installer");
                    if (Directory.Exists(target))
                    {
                        rootDir = target;
                        break;
                    }
                    else
                    {
                        rootDir = Path.Combine(rootDir, "..");
                    }
                }

                var instPath = Path.Combine(rootDir, "installerdata.xml");
                if (File.Exists(instPath))
                {
                    var ser = new XmlSerializer(typeof(GameInstallerData));
                    return (GameInstallerData)ser.Deserialize(XmlReader.Create(instPath));
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, $"Failed to deserialize game installer xml {dataPath}.");
        }

        return null;
    }

    internal GameLocalDataResponse GetLocalInstallerManifest(string id)
    {
        GameLocalDataResponse manifest;
        var manifestCacheFile = Path.Combine(installManifestCacheDir, Paths.GetSafePathName(id) + ".json");
        if (File.Exists(manifestCacheFile))
        {
            try
            {
                manifest = Serialization.FromJsonFile<GameLocalDataResponse>(manifestCacheFile);
                if (manifest != null)
                {
                    return manifest;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to read installer manifest cache file.");
            }
        }

        // There was a call to the Origin API here, but we can never get installer manifests again since the API was killed
        manifest = new GameLocalDataResponse
        {
            offerId = id,
            offerType = fakeOfferType
        };

        FileSystem.PrepareSaveFile(manifestCacheFile);
        File.WriteAllText(manifestCacheFile, Serialization.ToJson(manifest));
        return manifest;
    }

    public GameAction GetGamePlayTask(string installerDataPath)
    {
        var data = GetGameInstallerData(installerDataPath);
        if (data == null)
        {
            return null;
        }
        else
        {
            var launcher = data.runtime.launchers.FirstOrDefault(a => !a.trial);
            if (data.runtime.launchers.Count > 1)
            {
                if (System.Environment.Is64BitOperatingSystem)
                {
                    var s4 = data.runtime.launchers.FirstOrDefault(a => a.requires64BitOS && !a.trial);
                    if (s4 != null)
                    {
                        launcher = s4;
                    }
                }
                else
                {
                    var s3 = data.runtime.launchers.FirstOrDefault(a => !a.requires64BitOS && !a.trial);
                    if (s3 != null)
                    {
                        launcher = s3;
                    }
                }
            }

            var paths = GetPathFromPlatformPath(launcher.filePath);
            if (paths.CompletePath.Contains("://"))
            {
                return new GameAction
                {
                    Type = GameActionType.URL,
                    Path = paths.CompletePath
                };
            }
            else
            {
                var action = new GameAction
                {
                    Type = GameActionType.File
                };
                if (paths.Path.IsNullOrEmpty())
                {
                    action.Path = paths.CompletePath;
                    action.WorkingDir = Path.GetDirectoryName(paths.CompletePath);
                }
                else
                {
                    action.Path = paths.CompletePath;
                    action.WorkingDir = paths.Root;
                }

                return action;
            }
        }
    }

    public GameAction GetGamePlayTask(GameLocalDataResponse manifest)
    {
        var platform = manifest.publishing.softwareList.software.FirstOrDefault(a => a.softwarePlatform == "PCWIN");
        var playAction = new GameAction();
        if (string.IsNullOrEmpty(platform?.fulfillmentAttributes?.executePathOverride))
        {
            return null;
        }

        if (platform.fulfillmentAttributes.executePathOverride.Contains("://"))
        {
            playAction.Type = GameActionType.URL;
            playAction.Path = platform.fulfillmentAttributes.executePathOverride;
        }
        else
        {
            var executePath = GetPathFromPlatformPath(platform.fulfillmentAttributes.executePathOverride);
            if (executePath != null)
            {
                if (executePath.CompletePath.EndsWith("installerdata.xml", StringComparison.OrdinalIgnoreCase))
                {
                    return GetGamePlayTask(executePath.CompletePath);
                }
                else if (executePath.CompletePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    playAction.WorkingDir = executePath.Root;
                    playAction.Path = executePath.CompletePath;
                }
                else
                {
                    // This happens in case of Sims 4 for example, where executable path points to generic ClientFullBuild0.package file
                    // so the actual executable needs to be forced to installerdata.xaml resolution.
                    return GetGamePlayTask(Path.GetDirectoryName(executePath.CompletePath));
                }
            }
        }

        return playAction;
    }

    public GameAction GetGamePlayTaskForGameId(string gameId)
    {
        var installManifest = GetLocalInstallerManifest(gameId);
        if (installManifest.offerType == fakeOfferType)
        {
            return new GameAction
            {
                Type = GameActionType.URL,
                Path = EaApp.LibraryOpenUri
            };
        }
        else
        {
            return GetGamePlayTask(installManifest);
        }
    }

    public string GetInstallDirectory(GameLocalDataResponse localData)
    {
        var platform = localData.publishing.softwareList.software.FirstOrDefault(a => a.softwarePlatform == "PCWIN");
        if (platform == null)
        {
            return null;
        }

        var installPath = GetPathFromPlatformPath(platform.fulfillmentAttributes.installCheckOverride);
        if (installPath == null ||
            installPath.CompletePath.IsNullOrEmpty() ||
            !File.Exists(installPath.CompletePath))
        {
            return null;
        }

        var action = GetGamePlayTask(localData);
        if (action?.Type == GameActionType.File)
        {
            return action.WorkingDir;
        }
        else
        {
            return Path.GetDirectoryName(installPath.CompletePath);
        }
    }

    public Dictionary<string, GameMetadata> GetInstalledGames(CancellationToken cancelToken, List<GameMetadata> userGames)
    {
        var games = new Dictionary<string, GameMetadata>();
        foreach (var userGame in userGames)
        {
            if (cancelToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var newGame = new GameMetadata()
                {
                    Source = new MetadataNameProperty("EA app"),
                    GameId = userGame.GameId,
                    IsInstalled = true,
                    Platforms = [new MetadataSpecProperty("pc_windows")]
                };

                var localData = GetLocalInstallerManifest(userGame.GameId);
                if (localData.offerType == fakeOfferType)
                {
                    continue;
                }

                if (localData.offerType != "Base Game" && localData.offerType != "DEMO")
                {
                    continue;
                }

                newGame.Name = (localData.localizableAttributes?.displayName ?? localData.i18n?.displayName ?? localData.itemName).NormalizeGameName();
                var installDir = GetInstallDirectory(localData);
                if (installDir.IsNullOrEmpty())
                {
                    continue;
                }

                newGame.InstallDirectory = installDir;
                // Games can be duplicated if user has EA Play sub and also bought the game.
                if (!games.ContainsKey(newGame.GameId))
                {
                    games.Add(newGame.GameId, newGame);
                }
            }
            catch (Exception e) when (!Environment.IsDebugBuild)
            {
                logger.Error(e, $"Failed to import installed EA game {userGame.GameId}.");
            }
        }

        return games;
    }

    public List<GameMetadata> GetLibraryGames(CancellationToken cancelToken)
    {
        var games = new List<GameMetadata>();
        var manifests = new EaInstallerDataScanner().GetManifests(cancelToken);
        
        foreach (var manifest in manifests)
        {
            throw new NotImplementedException();
            
            /*
            games.Add(new GameMetadata()
            {
                Source = new MetadataNameProperty("EA app"),
                GameId = game.offerId,
                Name = gameName,
                LastActivity = usage?.lastSessionEndTimeStamp,
                Playtime = (ulong)(usage?.total ?? 0),
                Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") }
            });
            */
        }

        return games;
    }

    public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
    {
        var allGames = new List<GameMetadata>();
        if (!SettingsViewModel.Settings.ConnectAccount)
        {
            return allGames;
        }

        var installedGames = new Dictionary<string, GameMetadata>();
        Exception importError = null;

        try
        {
            allGames = GetLibraryGames(args.CancelToken);
            Logger.Debug($"Found {allGames.Count} library EA games.");
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to import linked account EA games details.");
            importError = e;
        }

        if (importError == null)
        {
            if (SettingsViewModel.Settings.ImportInstalledGames)
            {
                try
                {
                    installedGames = GetInstalledGames(args.CancelToken, allGames);
                    Logger.Debug($"Found {installedGames.Count} installed EA games.");
                    foreach (var installedGame in installedGames.Values)
                    {
                        var libraryGame = allGames.First(a => a.GameId == installedGame.GameId);
                        allGames.Remove(libraryGame);
                        installedGame.Playtime = libraryGame.Playtime;
                        installedGame.LastActivity = libraryGame.LastActivity;
                        allGames.Add(installedGame);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to import installed EA games.");
                    importError = e;
                }
            }

            if (!SettingsViewModel.Settings.ImportUninstalledGames)
            {
                allGames.RemoveAll(a => !a.IsInstalled);
            }
        }

        if (importError != null)
        {
            PlayniteApi.Notifications.Add(new NotificationMessage(
                                              ImportErrorMessageId,
                                              string.Format(PlayniteApi.Resources.GetString("LOCLibraryImportError"), Name) +
                                              System.Environment.NewLine + importError.Message,
                                              NotificationType.Error,
                                              () => OpenSettingsView()));
        }
        else
        {
            PlayniteApi.Notifications.Remove(ImportErrorMessageId);
        }

        return allGames;
    }

    public override IEnumerable<InstallController> GetInstallActions(GetInstallActionsArgs args)
    {
        if (args.Game.PluginId != Id)
        {
            yield break;
        }

        yield return new EaInstallController(args.Game, this);
    }

    public override IEnumerable<UninstallController> GetUninstallActions(GetUninstallActionsArgs args)
    {
        if (args.Game.PluginId != Id)
        {
            yield break;
        }

        yield return new EaUninstallController(args.Game, this);
    }

    public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
    {
        if (args.Game.PluginId != Id)
        {
            yield break;
        }

        yield return new EaPlayController(args.Game, this);
    }
}
