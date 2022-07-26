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
            yield return new MainMenuItem { MenuSection = "@", Description = "Replace paths", Action = ReplacePaths };
        }

        private void ReplacePaths(MainMenuItemActionArgs obj)
        {
            var findDialogResult = PlayniteApi.Dialogs.SelectString(@"Enter the path you want replaced. Only matches at the start of a path will be replaced, so include drive letters and the like.", "Enter path to be replaced", "");
            if (!findDialogResult.Result || string.IsNullOrWhiteSpace(findDialogResult.SelectedString))
                return;

            var replaceDialogResult = PlayniteApi.Dialogs.SelectString($@"Enter the full path you want to replace {findDialogResult.SelectedString} with.", "Enter new path", "");
            if (!replaceDialogResult.Result || string.IsNullOrWhiteSpace(replaceDialogResult.SelectedString))
                return;

            string normalizedFind = NormalizePath(findDialogResult.SelectedString);
            string replace = replaceDialogResult.SelectedString;

            if (normalizedFind.EndsWith(@"\") && !NormalizePath(replace).EndsWith(@"\"))
                replace += @"\";

            PlayniteApi.Dialogs.ActivateGlobalProgress((args) =>
            {
                var games = PlayniteApi.Database.Games;
                args.ProgressMaxValue = games.Count;
                int updated = 0;
                using (PlayniteApi.Database.BufferedUpdate())
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
                            updated++;
                            PlayniteApi.Database.Games.Update(game);
                            logger.Debug($"Updated path(s) for {game.Name}");
                        }
                    }
                }
                PlayniteApi.Dialogs.ShowMessage($"Updated paths for {updated} games!");
            }, new GlobalProgressOptions($"Replacing {normalizedFind} with {replace}…", true) { IsIndeterminate = false });
        }

        public static bool ReplacePathsForGame(Game game, string normalizedFind, string replace)
        {
            bool updated = false;
            if (ShouldReplace(game.InstallDirectory, normalizedFind, replace, out string newInstallDir))
            {
                game.InstallDirectory = newInstallDir;
                updated = true;
            }

            if (game.Roms == null)
                return updated;

            foreach (var rom in game.Roms)
            {
                if (ShouldReplace(rom.Path, normalizedFind, replace, out string newRomPath))
                {
                    rom.Path = newRomPath;
                    updated = true;
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