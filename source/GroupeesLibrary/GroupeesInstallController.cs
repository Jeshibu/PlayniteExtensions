using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace GroupeesLibrary
{
    //This was a nice idea, but I'm leaving it for now - the installers can be anything and the UI was too much of a headache:
    // - Installer.exe
    // - Archive with the game directory inside (ready to run once unarchived)
    // - Archive with installer inside
    // - Archive with installer directory inside
    // - Archive with archive(s) of any of the above flavors inside
    public class GroupeesInstallController : InstallController
    {
        public GroupeesInstallController(Game game, GroupeesLibrarySettings settings, IWebDownloader downloader, IPlayniteAPI playniteAPI, Plugin plugin) : base(game)
        {
            Name = "Groupees install controller";
            Settings = settings;
            Downloader = downloader;
            PlayniteAPI = playniteAPI;
            Plugin = plugin;
        }

        public GroupeesLibrarySettings Settings { get; }
        public IWebDownloader Downloader { get; }
        public IPlayniteAPI PlayniteAPI { get; }
        public Plugin Plugin { get; }
        private ILogger logger = LogManager.GetLogger();

        private bool Validate(out int gameId, out GameInstallInfo installData, out string errorMessage)
        {
            gameId = 0;
            installData = null;
            if (string.IsNullOrWhiteSpace(Settings.InstallationDirectory))
            {
                errorMessage = "Groupees install directory is not set. Please set it in the Addons settings menu (F9).";
                return false;
            }

            Directory.CreateDirectory(Settings.InstallationDirectory);

            if (!Settings.InstallData.TryGetValue(Game.GameId, out installData) || string.IsNullOrEmpty(installData?.DownloadUrl))
            {
                errorMessage = "No download URL found. Double-check if you've revealed the game in your Groupees purchases page and then run the import again.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private void LogAndDisplayError(string errorMessage, Exception ex = null)
        {
            if (ex == null)
                logger.Error(errorMessage);
            else
                logger.Error(ex, errorMessage);

            PlayniteAPI.Dialogs.ShowMessage(errorMessage, "Install error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        public override void Install(InstallActionArgs installArgs)
        {
            Game.IsInstalling = true;

            var result = Install();

            if (result.Status == InstallProcessStatus.Success)
            {
                Game.IsInstalling = false;
                Game.IsInstalled = true;
                Game.InstallDirectory = result.InstallDirectory;
                Game.IncludeLibraryPluginAction = true;
                var installData = Settings.InstallData[Game.GameId]; //installdata can be assumed to exist and gameid can be assumed to be an int because of the success status
                installData.InstallLocation = result.InstallDirectory;
                installData.RelativeExecutablePath = result.GameExePath;
            }
            else
            {
                if (result.Status == InstallProcessStatus.Failed)
                {
                    LogAndDisplayError(result.StatusReason, result.Exception);
                }
                else
                {
                    logger.Debug(result.StatusReason);
                }

                Game.IsInstalling = false;
                Game.IsInstalled = false;
                Game.InstallDirectory = null;
                if (Settings.InstallData.TryGetValue(Game.GameId, out var installData))
                {
                    installData.InstallLocation = null;
                }
            }
            Settings.Cookies = Downloader.Cookies.Cast<Cookie>().ToList();
            Plugin.SavePluginSettings(Settings);
        }

        public InstallProcessResult Install()
        {
            if (!Validate(out int gameId, out GameInstallInfo installData, out string errorMessage))
                return InstallProcessResult.Fail(errorMessage);

            logger.Debug($"Installing {Game.Name} from {installData.DownloadUrl}");

            Settings.Cookies.ForEach(Downloader.Cookies.Add);
            try
            {
                string downloadedFilePath = null;
                var downloadProcessResult = PlayniteAPI.Dialogs.ActivateGlobalProgress((args) =>
                {
                    DownloadProgressCallback updateProgress = (long downloaded, long total) =>
                    {
                        if (total == 0L)
                            return;
                        args.ProgressMaxValue = total;
                        args.CurrentProgressValue = downloaded;
                    };
                    downloadedFilePath = Downloader.DownloadFile(installData.DownloadUrl, targetFolder: Path.GetTempPath(), args.CancelToken, updateProgress);
                    logger.Debug($"File downloaded: {downloadedFilePath}");
                }, new GlobalProgressOptions($"Downloading {Game.Name}…", cancelable: true) { IsIndeterminate = false });

                if (downloadProcessResult.Error != null)
                {
                    return InstallProcessResult.Fail("Error in game download process", downloadProcessResult.Error);
                }

                FileInfo downloadedFile = null;
                if (downloadedFilePath == null || !(downloadedFile = new FileInfo(downloadedFilePath)).Exists)
                {
                    return InstallProcessResult.Fail($"Downloaded file not found. Path: {downloadedFilePath}");
                }

                switch (downloadedFile.Extension.ToLowerInvariant())
                {
                    case ".exe": return HandleInstaller(downloadedFilePath);
                    case ".zip":
                    case ".rar": return HandleZipFileInstall(downloadedFilePath);
                    default: return InstallProcessResult.Fail($"Unknown file type: {downloadedFile.Extension}");
                }
            }
            catch (Exception ex)
            {
                return InstallProcessResult.Fail("Unknown error in install process", ex);
            }
        }

        private InstallProcessResult HandleZipFileInstall(string archivePath)
        {
            //using (var archive = SharpCompress.Archives.ArchiveFactory.Open(archivePath))
            //{
            //    //if there's only one folder in the archive, use that as the install path
            //    var archiveFolderPathSegments = archive.Entries
            //        .Where(e => e.IsDirectory) //only directories; directory FullNames end with /
            //        .Select(e => e.Key.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            //        .ToList();
            //    var singleRootFolderName = archiveFolderPathSegments.SingleOrDefault(p => p.Length == 1)?[0];
            //    string unzipTargetPath = singleRootFolderName == null ? Path.Combine(Settings.InstallationDirectory, singleRootFolderName) : Settings.InstallationDirectory;
            //    string gameInstallDir = Path.Combine(Settings.InstallationDirectory, singleRootFolderName ?? Game.Name.ReplaceInvalidFileNameChars());
            //}
            return null;
        }

        private InstallProcessResult HandleInstaller(string installerPath)
        {
            logger.Debug($"Launching installer {installerPath}");
            PlayniteAPI.Dialogs.ShowMessage("About to launch the downloaded installer. Remember the path you install to, you'll need to enter it here after.");
            PlayniteAPI.Dialogs.ActivateGlobalProgress((args) =>
            {
                using (var installerProcess = Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = false, }))
                {
                    installerProcess.WaitForExit();
                    logger.Debug($"Installer process exited with code {installerProcess.ExitCode}");
                }
            }, new GlobalProgressOptions("Awaiting installer process end", cancelable: false) { IsIndeterminate = true });

            PlayniteAPI.Dialogs.ShowMessage($"Please select the folder {Game.Name} was installed to in the next window. Press cancel if the install failed.");
            var installationDirectory = PlayniteAPI.Dialogs.SelectFolder();
            if (string.IsNullOrEmpty(installationDirectory))
                return InstallProcessResult.Cancel("User cancelled out of post-installer install directory selection", installerPath);


            string[] exePaths = Directory.GetFiles(installationDirectory, "*.exe", SearchOption.AllDirectories);
            var exeOptions = exePaths.Select(s => new GenericItemOption { Name = s.Replace(installationDirectory, string.Empty) }).ToList();

            var selectedExe = PlayniteAPI.Dialogs.ChooseItemWithSearch(exeOptions,
                (s) => string.IsNullOrWhiteSpace(s) ? exeOptions : exeOptions.Where(o => o.Name.Contains(s)).ToList(),
                caption: $"Please select the .exe file that starts {Game.Name} in {installationDirectory}.");

            if (selectedExe == null)
                return InstallProcessResult.Cancel("User cancelled out of post-installer game exe selection", installerPath);

            return new InstallProcessResult
            {
                InstallDirectory = installationDirectory,
                GameExePath = selectedExe.Name,
                Status = InstallProcessStatus.Success,
                FilesToCleanUp = new List<string> { installerPath },
            };
        }

        public enum InstallProcessStatus
        {
            Failed,
            Canceled,
            Success,
        }

        public class InstallProcessResult
        {
            public List<string> FilesToCleanUp { get; set; } = new List<string>();
            public InstallProcessStatus Status { get; set; }
            public string StatusReason { get; set; }
            public string InstallDirectory { get; set; }
            public string GameExePath { get; set; }
            public Exception Exception { get; set; }

            public InstallProcessResult() { }

            public InstallProcessResult(InstallProcessStatus status, string statusReason, params string[] filesToCleanUp)
            {
                Status = status;
                StatusReason = statusReason;
                if (filesToCleanUp != null)
                    FilesToCleanUp.AddRange(filesToCleanUp);
            }

            public static InstallProcessResult Fail(string reason, params string[] filesToCleanUp)
            {
                return new InstallProcessResult(InstallProcessStatus.Failed, reason, filesToCleanUp);
            }
            public static InstallProcessResult Fail(string reason, Exception exception, params string[] filesToCleanUp)
            {
                return new InstallProcessResult(InstallProcessStatus.Failed, reason, filesToCleanUp) { Exception = exception };
            }
            public static InstallProcessResult Cancel(string reason, params string[] filesToCleanUp)
            {
                return new InstallProcessResult(InstallProcessStatus.Canceled, reason, filesToCleanUp);
            }
        }
    }
}