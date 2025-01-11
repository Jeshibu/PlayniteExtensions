using MutualGames.Models.Export;
using MutualGames.Models.Settings;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MutualGames
{
    public abstract class MutualGamesBaseImporter
    {
        protected readonly IPlayniteAPI playniteAPI;
        protected readonly MutualGamesSettings settings;
        protected readonly GameMatchingHelper matchingHelper = new GameMatchingHelper(new SteamIdUtility(), 2); //not going to use these args, but no other constructor for now
        protected readonly ILogger logger = LogManager.GetLogger();
        protected int updatedCount = 0;

        public MutualGamesBaseImporter(IPlayniteAPI playniteAPI, MutualGamesSettings settings)
        {
            this.playniteAPI = playniteAPI;
            this.settings = settings;
        }

        #region property getting and assigning

        protected DatabaseObject GetDatabaseItem(string friendName, string sourceName)
        {
            var propName = string.Format(settings.PropertyNameFormat.Trim(), friendName, sourceName);
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

        protected bool AddPropertyToGame(Game game, DatabaseObject databaseObject)
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

        protected IEnumerable<Game> GetMatchingGames(Guid libraryPluginId, List<ExternalGameData> friendGames, string friendName)
        {
            var output = new List<Game>();

            var sameLibraryGames = GetSameLibraryGames(libraryPluginId, out var otherLibraryGames);

            foreach (var friendGame in friendGames)
            {
                var sameLibraryMatchingGame = sameLibraryGames.FirstOrDefault(g => friendGame.Id == g.GameId);
                if (sameLibraryMatchingGame != null)
                    output.Add(sameLibraryMatchingGame);

                if (!otherLibraryGames.Any())
                    continue;

                var deflatedFriendGameName = matchingHelper.GetDeflatedName(friendGame.Name);
                foreach (var otherLibraryGame in otherLibraryGames)
                {
                    var deflatedLocalGameName = matchingHelper.GetDeflatedName(otherLibraryGame.Name);
                    if (deflatedFriendGameName.Equals(deflatedLocalGameName, StringComparison.InvariantCultureIgnoreCase))
                        output.Add(otherLibraryGame);
                }
            }

            logger.Info($"Matching {friendName}'s games: {friendGames.Count} of their games matched {output.Count} in the local library");

            return output;
        }

        private List<Game> GetSameLibraryGames(Guid libraryPluginId, out List<Game> otherLibraryGames)
        {
            var sameLibrary = new List<Game>();
            otherLibraryGames = new List<Game>();

            foreach (var game in playniteAPI.Database.Games)
            {
                if (game.PluginId == libraryPluginId)
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
