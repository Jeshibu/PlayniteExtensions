using MutualGames.Clients;
using Playnite.SDK;
using System;
using System.Linq;
using System.Threading;
using MutualGames.Models.Settings;
using System.IO;
using Newtonsoft.Json;
using MutualGames.Models.Export;
using PlayniteExtensions.Common;
using System.Runtime;

namespace MutualGames
{
    public class MutualGamesFileImporter : MutualGamesBaseImporter
    {
        public MutualGamesFileImporter(IPlayniteAPI playniteAPI, MutualGamesSettings settings) : base(playniteAPI, settings) { }

        public void Import()
        {
            if (!TryGetFile(out var file))
                return;

            var friendName = GetFriendName(file);
            if (friendName == null)
                return;

            var result = playniteAPI.Dialogs.ActivateGlobalProgress(a =>
            {
                a.Text = $"Reading {file.FullName}";
                var fileContentString = File.ReadAllText(file.FullName);
                var fileContent = JsonConvert.DeserializeObject<ExportRoot>(fileContentString);

                matchingHelper.GetDeflatedNames(playniteAPI.Database.Games.Select(g => g.Name));
                matchingHelper.GetDeflatedNames(fileContent.Games.Select(g => g.Name));

                var pluginsById = fileContent.LibraryPlugins.ToDictionary(p => p.Id, p => p.Name);
                var gamesByPluginId = fileContent.Games.GroupBy(g => g.PluginId);
                var dbItem = GetDatabaseItem(friendName);

                using (playniteAPI.Database.BufferedUpdate())
                {
                    foreach (var grouping in gamesByPluginId)
                    {
                        if (a.CancelToken.IsCancellationRequested) break;

                        if (!pluginsById.TryGetValue(grouping.Key, out var pluginName))
                            pluginName = "Playnite";

                        var friendGames = grouping.ToList();
                        a.Text = $"Importing {friendGames.Count} from {pluginName} for {friendName}";

                        try
                        {
                            var matchingGames = GetMatchingGames(grouping.Key, friendGames, friendName);
                            foreach (var matchingGame in matchingGames)
                                if (AddPropertyToGame(matchingGame, dbItem))
                                    playniteAPI.Database.Games.Update(matchingGame);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Error while getting games for {pluginName} for {friendName}");
                            //TODO: display error to user
                        }
                        a.CurrentProgressValue++;
                    }
                }
            }, new GlobalProgressOptions("Importing friend games", cancelable: true) { IsIndeterminate = true });

            playniteAPI.Dialogs.ShowMessage($"Imported {updatedCount} new friends' games.", "Mutual Games import done");
        }

        private bool TryGetFile(out FileInfo file)
        {
            file = null;

            var filePath = playniteAPI.Dialogs.SelectFile(MutualGames.ExportFileFilter);
            if (filePath == null)
                return false;

            file = new FileInfo(filePath);
            return file.Exists;
        }

        private string GetFriendName(FileInfo file)
        {
            string examplePropertyName = string.Format(settings.PropertyNameFormat, "FRIENDNAME");
            string defaultFriendName = file.Name.TrimEnd(file.Extension);
            var friendNameResult = playniteAPI.Dialogs.SelectString($"Enter your friend's name. Mutual games will be added to {settings.ImportTo} named \"{examplePropertyName}\"", "Enter friend name", defaultFriendName);
            if (!friendNameResult.Result || string.IsNullOrWhiteSpace(friendNameResult.SelectedString))
                return null;

            return friendNameResult.SelectedString;
        }
    }
}
