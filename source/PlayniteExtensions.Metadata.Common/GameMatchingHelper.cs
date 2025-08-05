using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteExtensions.Metadata.Common;

public class GameMatchingHelper(IExternalDatabaseIdUtility externalDatabaseIdUtility, int maxDegreeOfParallelism)
{
    private ConcurrentDictionary<DbId, IList<Game>> GamesById { get; } = new();

    public ConcurrentDictionary<string, string> DeflatedNames { get; } = new();
    public IExternalDatabaseIdUtility ExternalDatabaseIdUtility { get; } = externalDatabaseIdUtility;
    public int MaxDegreeOfParallelism { get; } = maxDegreeOfParallelism;

    private readonly SortableNameConverter sortableNameConverter = new(numberLength: 1, removeEditions: true);

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

        Parallel.ForEach(library, options, AddGame);
    }

    private void AddGame(Game game)
    {
        var dbIDs = ExternalDatabaseIdUtility.GetIdsFromGame(game).ToList();

        if (dbIDs.Any())
            foreach (var dbID in dbIDs)
                AddGameById(dbID, game);
        else
            AddGameByName(game);
    }

    private IList<Game> AddGameById(DbId key, Game game)
    {
        return GamesById.AddOrUpdate(key, [game], (DbId _, IList<Game> existing) =>
        {
            if (!existing.Contains(game))
                existing.Add(game);

            return existing;
        });
    }

    private IList<Game> AddGameByName(Game game) => AddGameById(DbId.NoDb(GetDeflatedName(game.Name)), game);

    public bool TryGetGamesById(DbId key, out IList<Game> games)
    {
        if (string.IsNullOrWhiteSpace(key.Id))
        {
            games = [];
            return false;
        }

        return GamesById.TryGetValue(key, out games);
    }

    public bool TryGetGamesByName(string name, out IList<Game> games)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            games = [];
            return false;
        }

        var key = DbId.NoDb(GetDeflatedName(name));
        return TryGetGamesById(key, out games);
    }
}
