using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlayniteExtensions.Metadata.Common
{
    public class GameMatchingHelper
    {
        public GameMatchingHelper(IExternalDatabaseIdUtility externalDatabaseIdUtility, int maxDegreeOfParallelism)
        {
            ExternalDatabaseIdUtility = externalDatabaseIdUtility;
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            GamesById = new ConcurrentDictionary<(ExternalDatabase Database, string Id), IList<Game>>();
        }

        private ConcurrentDictionary<(ExternalDatabase Database, string Id), IList<Game>> GamesById { get; }

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
            return DeflatedNames.GetOrAdd(name, x => sortableNameConverter.Convert(x).Deflate().Normalize(NormalizationForm.FormKD));
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
                    AddGameById((ExternalDatabase.None, GetDeflatedName(game.Name)), game);
                }
            });
        }

        private IList<Game> AddGameById((ExternalDatabase Database, string Id) key, Game game)
        {
            key = Transform(key);
            return GamesById.AddOrUpdate(key, new List<Game> { game }, ((ExternalDatabase Database, string Id) _, IList<Game> existing) =>
            {
                if (!existing.Contains(game))
                    existing.Add(game);

                return existing;
            });
        }

        public bool TryGetGamesById((ExternalDatabase Database, string Id) key, out IList<Game> games)
        {
            if (string.IsNullOrWhiteSpace(key.Id))
            {
                games = new List<Game>();
                return false;
            }

            key = Transform(key);
            return GamesById.TryGetValue(key, out games);
        }

        public bool TryGetGamesByName(string name, out IList<Game> games)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                games = new List<Game>();
                return false;
            }

            var key = (ExternalDatabase.None, GetDeflatedName(name));
            return TryGetGamesById(key, out games);
        }

        private (ExternalDatabase Database, string Id) Transform((ExternalDatabase Database, string Id) key)
        {
            key.Id = key.Id.ToLowerInvariant();
            return key;
        }
    }
}
