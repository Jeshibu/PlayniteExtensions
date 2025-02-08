using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteExtensions.Metadata.Common
{
    public class GameMatchingHelper
    {
        public GameMatchingHelper(IExternalDatabaseIdUtility externalDatabaseIdUtility, int maxDegreeOfParallelism)
        {
            ExternalDatabaseIdUtility = externalDatabaseIdUtility;
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            GamesById = new ConcurrentDictionary<DbId, IList<Game>>();
        }

        private ConcurrentDictionary<DbId, IList<Game>> GamesById { get; }

        public ConcurrentDictionary<string, string> DeflatedNames { get; } = new ConcurrentDictionary<string, string>();
        public IExternalDatabaseIdUtility ExternalDatabaseIdUtility { get; }
        public int MaxDegreeOfParallelism { get; }

        private SortableNameConverter sortableNameConverter = new SortableNameConverter(numberLength: 1, removeEditions: true);

        public HashSet<string> GetDeflatedNames(IEnumerable<string> names)
        {
            return new HashSet<string>(names.Select(GetDeflatedName), StringComparer.InvariantCultureIgnoreCase);
        }

        public string GetDeflatedName(string name)
        {
            return DeflatedNames.GetOrAdd(name, GenerateDeflatedName);
        }

        private string GenerateDeflatedName(string name)
        {
            return sortableNameConverter
                .Convert(name)
                .Deflate()
                .Normalize(NormalizationForm.FormKD);
        }

        public void Prepare(IEnumerable<Game> library, CancellationToken cancellationToken)
        {
            var options = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = MaxDegreeOfParallelism };

            Parallel.ForEach(library, options, game =>
            {
                var dbIDs = ExternalDatabaseIdUtility.GetIdsFromGame(game).ToList();

                if (dbIDs.Any())
                {
                    foreach (var dbID in dbIDs)
                        AddGameById(dbID, game);
                }
                else
                {
                    AddGameById(DbId.NoDb(GetDeflatedName(game.Name)), game);
                }
            });
        }

        private IList<Game> AddGameById(DbId key, Game game)
        {
            return GamesById.AddOrUpdate(key, new List<Game> { game }, (DbId _, IList<Game> existing) =>
            {
                if (!existing.Contains(game))
                    existing.Add(game);

                return existing;
            });
        }

        public bool TryGetGamesById(DbId key, out IList<Game> games)
        {
            if (string.IsNullOrWhiteSpace(key.Id))
            {
                games = new List<Game>();
                return false;
            }

            return GamesById.TryGetValue(key, out games);
        }

        public bool TryGetGamesByName(string name, out IList<Game> games)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                games = new List<Game>();
                return false;
            }

            var key = DbId.NoDb(GetDeflatedName(name));
            return TryGetGamesById(key, out games);
        }
    }
}
