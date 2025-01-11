using PCGamingWikiBulkImport.DataCollection;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
            var fetch = GetMatchingGamesFunction(searchResult);

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

        private Func<int, CargoResultRoot<CargoResultGame>> GetMatchingGamesFunction(PCGamingWikiSelectedValues selected)
        {
            switch (selected.FieldInfo.FieldType)
            {
                case CargoFieldType.ListOfString:
                    var wa = selected.FieldInfo.ValueWorkaround(selected.SelectedValues.First());
                    if (wa.UseLike)
                        return offset => CargoQuery.GetGamesByHoldsLike(selected.FieldInfo.Table, selected.FieldInfo.Field, wa.Value, offset);
                    else
                        return offset => CargoQuery.GetGamesByHolds(selected.FieldInfo.Table, selected.FieldInfo.Field, wa.Value, offset);
                case CargoFieldType.String:
                    return offset => CargoQuery.GetGamesByExactValues(selected.FieldInfo.Table, selected.FieldInfo.Field, selected.SelectedValues, offset);
                default:
                    throw new ArgumentException($"Invalid selected value field info type: {selected.FieldInfo.FieldType}");
            }
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
            var counts = CargoQuery.GetValueCounts(field.Table, field.Field, searchString).ToList();
            foreach (var c in counts)
            {
                c.Value = WebUtility.HtmlDecode(c.Value);
            }
            return counts;
        }

        private GameDetails ToGameDetails(CargoResultGame g)
        {
            var name = WebUtility.HtmlDecode(g.Name);
            var slug = TitleToSlug(name);
            var game = new GameDetails
            {
                Id = slug,
                Names = new List<string> { name },
                Url = $"https://www.pcgamingwiki.com/wiki/{slug}",
            };

            game.Platforms = g.OS?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).SelectMany(PlatformUtility.GetPlatforms).ToList();
            game.ReleaseDate = GetReleaseDate(g.Released);

            if (!string.IsNullOrWhiteSpace(g.SteamID))
                game.ExternalIds.AddRange(SplitIds(g.SteamID, ExternalDatabase.Steam));

            if (!string.IsNullOrWhiteSpace(g.GOGID))
                game.ExternalIds.AddRange(SplitIds(g.GOGID, ExternalDatabase.GOG));

            return game;
        }

        private ReleaseDate? GetReleaseDate(string releaseDateString)
        {
            var releaseDateStrings = releaseDateString?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (releaseDateStrings == null || releaseDateStrings.Length == 0)
                return null;

            var releaseDates = releaseDateStrings.Select(StringExtensions.ParseReleaseDate).Where(d => d.HasValue).Select(d => d.Value).ToList();
            if (releaseDates.Any())
                return releaseDates.Min();

            return null;
        }

        public static string TitleToSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return title;

            var sb = new StringBuilder();
            foreach (char c in title)
                sb.Append(EscapeSlugCharacter(c));

            return sb.ToString();
        }

        private static string EscapeSlugCharacter(char c)
        {
            if (char.IsLetterOrDigit(c))
                return c.ToString();

            switch (c)
            {
                case ' ':
                    return "_";
                case ':':
                case '-':
                case '.':
                case '/':
                case '~':
                case ';':
                    return c.ToString();
                default:
                    return WebUtility.UrlEncode(c.ToString());
            };
        }

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
