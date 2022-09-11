using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PathReplacer
{
    public class PathReplacer : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("1184bf20-1951-4072-862f-984827ce2dba");

        public PathReplacer(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties
            {
                HasSettings = false
            };
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem { MenuSection = "@Path Replacer", Description = "Replace paths for all games", Action = ReplacePathsForAllGames };
            yield return new MainMenuItem { MenuSection = "@Path Replacer", Description = "Replace paths for visible games", Action = ReplacePathsForVisibleGames };
            yield return new MainMenuItem { MenuSection = "@Path Replacer", Description = "Replace paths for selected games", Action = ReplacePathsForSelectedGames };
        }

        private void ReplacePathsForSelectedGames(MainMenuItemActionArgs args)
        {
            ReplacePaths(PlayniteApi.MainView.SelectedGames.ToList());
        }

        private void ReplacePathsForVisibleGames(MainMenuItemActionArgs args)
        {
            ReplacePaths(PlayniteApi.MainView.FilteredGames);
        }

        private void ReplacePathsForAllGames(MainMenuItemActionArgs args)
        {
            ReplacePaths(PlayniteApi.Database.Games);
        }

        private void ReplacePaths(ICollection<Game> games)
        {
            var findDialogResult = PlayniteApi.Dialogs.SelectString(@"Enter the path you want replaced. Only matches at the start of a path will be replaced, so include drive letters and the like.", "Enter path to be replaced", "");
            if (!findDialogResult.Result || string.IsNullOrWhiteSpace(findDialogResult.SelectedString))
                return;

            var replaceDialogResult = PlayniteApi.Dialogs.SelectString($@"Enter the full path you want to replace {findDialogResult.SelectedString} with.", "Enter new path", "");
            if (!replaceDialogResult.Result || string.IsNullOrWhiteSpace(replaceDialogResult.SelectedString))
                return;

            var includeEmulatorsResult = PlayniteApi.Dialogs.ShowMessage("Also execute this replacement for emulators and auto-scan configurations?", "Execute for emulators?", System.Windows.MessageBoxButton.YesNoCancel);
            if (includeEmulatorsResult == System.Windows.MessageBoxResult.Cancel)
                return;

            bool includeEmulators = includeEmulatorsResult == System.Windows.MessageBoxResult.Yes;

            string normalizedFind = NormalizePath(findDialogResult.SelectedString);
            string replace = replaceDialogResult.SelectedString;

            if (normalizedFind.EndsWith(@"\") && !NormalizePath(replace).EndsWith(@"\"))
                replace += @"\";

            PlayniteApi.Dialogs.ActivateGlobalProgress((args) =>
            {
                args.ProgressMaxValue = games.Count;
                if (includeEmulators)
                    args.ProgressMaxValue += PlayniteApi.Database.Emulators.Count + PlayniteApi.Database.GameScanners.Count;

                int updatedGames = 0, updatedEmulators = 0, updatedAutoScanDirs = 0;
                var buffer = PlayniteApi.Database.BufferedUpdate();
                try
                {
                    int currentProgress = 0;

                    foreach (var game in games)
                    {
                        if (args.CancelToken.IsCancellationRequested)
                            return;

                        currentProgress++;
                        if (currentProgress % 10 == 0)
                            args.CurrentProgressValue = currentProgress;

                        if (ReplacePathsForGame(game, normalizedFind, replace))
                        {
                            updatedGames++;
                            PlayniteApi.Database.Games.Update(game);
                            logger.Debug($"Updated path(s) for game: {game.Name}");
                        }
                    }

                    if (includeEmulators)
                    {
                        foreach (var emulator in PlayniteApi.Database.Emulators)
                        {
                            if (args.CancelToken.IsCancellationRequested)
                                return;

                            currentProgress++;
                            args.CurrentProgressValue = currentProgress;

                            if (ReplacePathsForEmulator(emulator, normalizedFind, replace))
                            {
                                updatedEmulators++;
                                PlayniteApi.Database.Emulators.Update(emulator);
                                logger.Debug($"Updated path(s) for emulator: {emulator.Name}");
                            }
                        }

                        foreach (var gameScanner in PlayniteApi.Database.GameScanners)
                        {
                            if (args.CancelToken.IsCancellationRequested)
                                return;

                            currentProgress++;
                            args.CurrentProgressValue = currentProgress;

                            if (ShouldReplace(gameScanner.Directory, normalizedFind, replace, out string newDir))
                            {
                                updatedAutoScanDirs++;
                                gameScanner.Directory = newDir;
                                PlayniteApi.Database.GameScanners.Update(gameScanner);
                                logger.Debug($"Updated path for autscan config: {gameScanner.Name}");
                            }
                        }
                    }

                    PlayniteApi.Dialogs.ShowMessage($"Updated paths for {updatedGames} games, {updatedEmulators} emulators, and {updatedAutoScanDirs} auto-scan configurations!");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error while replacing paths");
                    PlayniteApi.Notifications.Add("pathreplacer-error", $"Error while replacing paths: {ex.Message}", NotificationType.Error);
                }
                finally
                {
                    buffer.Dispose();
                }
            }, new GlobalProgressOptions($"Replacing {normalizedFind} with {replace}…", true) { IsIndeterminate = false });
        }

        public static bool ReplacePathsForEmulator(Emulator emulator, string normalizedFind, string replace)
        {
            bool updated = false;
            if (ShouldReplace(emulator.InstallDir, normalizedFind, replace, out string newInstallDir))
            {
                emulator.InstallDir = newInstallDir;
                updated = true;
            }

            if (emulator.CustomProfiles == null)
                return updated;

            foreach (var profile in emulator.CustomProfiles)
            {
                if (ShouldReplace(profile.Executable, normalizedFind, replace, out string newExecutable))
                {
                    profile.Executable = newExecutable;
                    updated = true;
                }
            }

            return updated;
        }

        public static bool ReplacePathsForGame(Game game, string normalizedFind, string replace)
        {
            bool updated = false;
            if (ShouldReplace(game.InstallDirectory, normalizedFind, replace, out string newInstallDir))
            {
                game.InstallDirectory = newInstallDir;
                updated = true;
            }

            if (game.Roms != null)
            {
                foreach (var rom in game.Roms)
                {
                    if (ShouldReplace(rom.Path, normalizedFind, replace, out string newRomPath))
                    {
                        rom.Path = newRomPath;
                        updated = true;
                    }
                }
            }

            if (updated)
                game.Modified = DateTime.Now;

            return updated;
        }

        public static string NormalizePath(string path)
        {
            return path?.Replace('/', '\\');
        }

        public static bool ShouldReplace(string currentPath, string normalizedFind, string replace, out string newValue)
        {
            string normalizedCurrentPath = NormalizePath(currentPath);
            if (normalizedCurrentPath != null && normalizedCurrentPath.StartsWith(normalizedFind, StringComparison.InvariantCultureIgnoreCase))
            {
                newValue = replace + currentPath.Substring(normalizedFind.Length);
                return true;
            }

            newValue = null;
            return false;
        }
    }
}