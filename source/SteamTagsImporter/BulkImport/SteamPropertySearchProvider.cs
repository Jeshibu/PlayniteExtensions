using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SteamTagsImporter.BulkImport;

public class SteamPropertySearchProvider(SteamSearch steamSearch) : ISearchableDataSourceWithDetails<SteamProperty, IEnumerable<GameDetails>>
{
    private readonly ILogger logger = LogManager.GetLogger();
    private SteamProperty[] SteamProperties => field ??= steamSearch.GetProperties().ToArray();

    public IEnumerable<GameDetails> GetDetails(SteamProperty prop, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        logger.Info($"Getting list of games for {prop}");
        int start = 0, total = 0;
        var games = new List<GameDetails>();
        if (progressArgs != null)
            progressArgs.IsIndeterminate = false;

        do
        {
            var searchResult = steamSearch.SearchGames(prop.Param, prop.Value, start);
            total = searchResult.TotalCount;

            games.AddRange(steamSearch.ParseSearchResultHtml(searchResult.ResultsHtml));

            start += 50;

            if (progressArgs != null)
            {
                var progressText = $"Downloading {prop.Name}… {games.Count}/{total}";
                progressArgs.ProgressMaxValue = searchResult.TotalCount;
                progressArgs.CurrentProgressValue = games.Count;
                progressArgs.Text = progressText;
                logger.Info(progressText);
                logger.Info($"Actual downloaded game count: {games.Count}");
            }
        } while (start < total && progressArgs?.CancelToken.IsCancellationRequested != true);

        //deduplicate
        var groupedGames = games.GroupBy(g => g.Url);
        foreach (var gr in groupedGames)
        {
            var gamesInGroup = gr.ToList();
            if (gamesInGroup.Count > 1)
                logger.Info($"Found {gamesInGroup.Count} games for {gamesInGroup[0]} - {gr.Key}");

            yield return gamesInGroup[0];
        }
    }

    public IEnumerable<SteamProperty> Search(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(query))
            return SteamProperties;

        return SteamProperties.Where(sp => StringContains(sp.Name, query) || StringContains(sp.Category, query));
    }

    private static bool StringContains(string str, string query)
    {
        if(str == null)
            return false;

        return str.Contains(query, StringComparison.InvariantCultureIgnoreCase);
    }

    public GenericItemOption<SteamProperty> ToGenericItemOption(SteamProperty item)
    {
        return new GenericItemOption<SteamProperty>(item)
        {
            Name = item.Name,
            Description = item.Category
        };
    }
}
