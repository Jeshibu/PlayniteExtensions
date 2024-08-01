using MutualGames.Clients;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MutualGames
{
    public class MutualGamesImporter
    {
        private readonly IPlayniteAPI playniteAPI;
        private readonly MutualGamesSettings settings;
        private readonly IFriendsGamesClient[] clients;
        private readonly GameMatchingHelper matchingHelper = new GameMatchingHelper();
        private readonly ILogger logger = LogManager.GetLogger();
        private int updatedCount = 0;

        public MutualGamesImporter(IPlayniteAPI playniteAPI, MutualGamesSettings settings, IEnumerable<IFriendsGamesClient> clients)
        {
            this.playniteAPI = playniteAPI;
            this.settings = settings;
            this.clients = clients.ToArray();
        }

        public void Import()
        {
            var result = playniteAPI.Dialogs.ActivateGlobalProgress(a =>
            {
                a.ProgressMaxValue = settings.FriendIdentities.Items.SelectMany(fg => fg.Identities).Count() + 1;

                matchingHelper.GetDeflatedNames(playniteAPI.Database.Games.Select(g => g.Name));
                a.CurrentProgressValue++;

                using (playniteAPI.Database.BufferedUpdate())
                {
                    foreach (var friendIdentityGrouping in settings.FriendIdentities.Items)
                    {
                        if (a.CancelToken.IsCancellationRequested) break;

                        var dbItem = GetDatabaseItem(friendIdentityGrouping.FriendName);

                        foreach (var friend in friendIdentityGrouping.Identities)
                        {
                            a.Text = $"Getting games for {friendIdentityGrouping.FriendName} ({friend.Source} - {friend.Name})";
                            try
                            {
                                var matchingGames = GetMatchingGames(friend, a.CancelToken);
                                foreach (var matchingGame in matchingGames)
                                    if (AddPropertyToGame(matchingGame, dbItem))
                                        playniteAPI.Database.Games.Update(matchingGame);
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, $"Error while getting games for {friend.DisplayText}");
                                //TODO: display error to user
                            }
                            a.CurrentProgressValue++;
                        }
                    }
                }
            }, new GlobalProgressOptions("Importing friend games", cancelable: true) { IsIndeterminate = false });

            playniteAPI.Dialogs.ShowMessage($"Imported {updatedCount} new friends' games.", "Mutual Games import done");
        }

        #region property getting and assigning

        private DatabaseObject GetDatabaseItem(string friendName)
        {
            var propName = string.Format(settings.PropertyNameFormat.Trim(), friendName);
            var existingProperty = GetDatabaseCollectionToImportTo().FirstOrDefault(x => propName.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase));
            return existingProperty ?? CreateProperty(propName);
        }

        private IEnumerable<DatabaseObject> GetDatabaseCollectionToImportTo()
        {
            switch (settings.ImportTo)
            {
                case GameField.Categories: return playniteAPI.Database.Categories;
                case GameField.Tags: return playniteAPI.Database.Tags;
                default: throw new NotImplementedException();
            }
        }

        private DatabaseObject CreateProperty(string propName)
        {
            switch (settings.ImportTo)
            {
                case GameField.Categories: return playniteAPI.Database.Categories.Add(propName);
                case GameField.Tags: return playniteAPI.Database.Tags.Add(propName);
                default: throw new NotImplementedException();
            }
        }

        private bool AddPropertyToGame(Game game, DatabaseObject databaseObject)
        {
            var idList = GetIdList(game);
            var added = idList.AddMissing(databaseObject.Id);
            if (added)
            {
                game.Modified = DateTime.Now;
                updatedCount++;
            }
            return added;
        }

        private IList<Guid> GetIdList(Game game)
        {
            switch (settings.ImportTo)
            {
                case GameField.Categories: return game.CategoryIds ?? (game.CategoryIds = new List<Guid>());
                case GameField.Tags: return game.TagIds ?? (game.TagIds = new List<Guid>());
                default: throw new NotImplementedException();
            }
        }

        #endregion property getting and assigning

        #region game matching

        private IEnumerable<Game> GetMatchingGames(FriendInfo friend, CancellationToken cancellationToken)
        {
            var output = new List<Game>();
            var client = clients.FirstOrDefault(c => c.Source == friend.Source);
            if (client == null) return new Game[0];

            var sameLibraryGames = GetSameLibraryGames(client, out var otherLibraryGames);

            var friendGames = client.GetFriendGames(friend, cancellationToken).ToList();
            foreach (var friendGame in friendGames)
            {
                var samePluginLibraryGames = playniteAPI.Database.Games.Where(g => g.PluginId == client.PluginId);

                var sameLibraryMatchingGame = sameLibraryGames.FirstOrDefault(g => friendGame.Id == g.GameId);
                if (sameLibraryMatchingGame != null)
                    output.Add(sameLibraryMatchingGame);

                if (!otherLibraryGames.Any())
                    continue;

                var deflatedFriendGameNames = matchingHelper.GetDeflatedNames(friendGame.Names);
                foreach (var otherLibraryGame in otherLibraryGames)
                {
                    var deflatedLocalGameName = matchingHelper.GetDeflatedName(otherLibraryGame.Name);
                    if (deflatedFriendGameNames.Contains(deflatedLocalGameName, StringComparer.InvariantCultureIgnoreCase))
                        output.Add(otherLibraryGame);
                }
            }

            logger.Info($"Matching {friend.DisplayText}'s games: {friendGames.Count} of their games matched {output.Count} in the local library");

            return output;
        }

        private List<Game> GetSameLibraryGames(IFriendsGamesClient client, out List<Game> otherLibraryGames)
        {
            var sameLibrary = new List<Game>();
            otherLibraryGames = new List<Game>();

            foreach (var game in playniteAPI.Database.Games)
            {
                if (game.PluginId == client.PluginId)
                {
                    sameLibrary.Add(game);
                    continue;
                }

                switch (settings.CrossLibraryImportMode)
                {
                    case CrossLibraryImportMode.SameLibraryOnly: continue;
                    case CrossLibraryImportMode.ImportAll:
                        otherLibraryGames.Add(game);
                        break;
                    case CrossLibraryImportMode.ImportAllWithFeature:
                        if (game.FeatureIds?.Contains(settings.ImportCrossLibraryFeatureId) == true)
                            otherLibraryGames.Add(game);
                        break;
                }
            }

            return sameLibrary;
        }

        #endregion game matching
    }
}
