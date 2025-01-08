using PCGamingWikiBulkImport.DataCollection;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace PCGamingWikiBulkImport
{
    public class PCGamingWikiPropertySearchProvider : ISearchableDataSourceWithDetails<PCGamingWikiSelectedValues, IEnumerable<GameDetails>>
    {
        private readonly CargoTables Tables = new CargoTables();

        public PCGamingWikiPropertySearchProvider(ICargoQuery cargoQuery, IPlatformUtility platformUtility)
        {
            CargoQuery = cargoQuery;
            PlatformUtility = platformUtility;
        }

        private ICargoQuery CargoQuery { get; }
        private IPlatformUtility PlatformUtility { get; }
        private ILogger Logger { get; }

        public IEnumerable<GameDetails> GetDetails(PCGamingWikiSelectedValues searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
        {
            Func<int, CargoResultRoot<CargoResultGame>> fetch;
            if (searchResult.FieldInfo.HasReferenceTable)
                fetch = (int o) => CargoQuery.GetGamesByHolds(searchResult.FieldInfo.Table, searchResult.FieldInfo.Field, searchResult.SelectedValues.First(), o);
            else
                fetch = (int o) => CargoQuery.GetGamesByExactValues(searchResult.FieldInfo.Table, searchResult.FieldInfo.Field, searchResult.SelectedValues, o);

            var output = new List<GameDetails>();

            try
            {
                int offset = 0, resultCount, limit;
                do
                {
                    var result = fetch(offset);
                    resultCount = result.CargoQuery.Count;
                    limit = result.Limits.CargoQuery;
                    offset += limit;
                    output.AddRange(result.CargoQuery.Select(r => r.Title).Select(ToGameDetails));
                }
                while (resultCount > 0 && resultCount == limit);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting games");
            }

            return output;
        }

        public IEnumerable<PCGamingWikiSelectedValues> Search(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return ToSelectedValues(Tables.Fields);

            var matching = Tables.Fields.Where(f => f.FieldDisplayName.Contains(query, StringComparison.InvariantCultureIgnoreCase));
            return ToSelectedValues(matching);
        }

        private static IEnumerable<PCGamingWikiSelectedValues> ToSelectedValues(IEnumerable<CargoFieldInfo> fields)
        {
            return fields.Select(f => new PCGamingWikiSelectedValues { FieldInfo = f });
        }

        public GenericItemOption<PCGamingWikiSelectedValues> ToGenericItemOption(PCGamingWikiSelectedValues item)
        {
            return new GenericItemOption<PCGamingWikiSelectedValues>(item) { Name = item.FieldInfo.FieldDisplayName };
        }

        public IEnumerable<ItemCount> GetCounts(CargoFieldInfo field, string searchString)
        {
            return CargoQuery.GetValueCounts(field.Table, field.Field, searchString);
        }

        private GameDetails ToGameDetails(CargoResultGame g)
        {
            var slug = TitleToSlug(g.Name);
            var game = new GameDetails
            {
                Id = slug,
                Names = new List<string> { g.Name },
                Url = $"https://www.pcgamingwiki.com/wiki/{slug}",
            };

            game.Platforms = g.OS?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).SelectMany(PlatformUtility.GetPlatforms).ToList();
            game.ReleaseDate = g.Released.ParseReleaseDate(Logger);

            if (!string.IsNullOrWhiteSpace(g.SteamID))
                game.ExternalIds.AddRange(SplitIds(g.SteamID, ExternalDatabase.Steam));

            if (!string.IsNullOrWhiteSpace(g.GOGID))
                game.ExternalIds.AddRange(SplitIds(g.GOGID, ExternalDatabase.GOG));

            return game;
        }

        private static string TitleToSlug(string title) => WebUtility.UrlEncode(title.Replace(' ', '_'));

        private static IEnumerable<(ExternalDatabase, string)> SplitIds(string str, ExternalDatabase db)
        {
            if (string.IsNullOrWhiteSpace(str))
                yield break;

            var ids = str.Split(',');
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                yield return (db, id.Trim());
            }
        }
    }
}
