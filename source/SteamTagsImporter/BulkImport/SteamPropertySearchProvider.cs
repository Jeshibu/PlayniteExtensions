using ComposableAsync;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using PlayniteExtensions.Common;
using RateLimiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SteamTagsImporter.BulkImport;

public class SteamPropertySearchProvider(SteamSearch steamSearch) : IBulkPropertyImportDataSource<SteamProperty>
{
    private readonly ILogger _logger = LogManager.GetLogger();
    private SteamProperty[] SteamProperties => field ??= steamSearch.GetProperties().ToArray();

    public IEnumerable<GameDetails> GetDetails(SteamProperty prop, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        _logger.Info($"Getting list of games for {prop}");
        int start = 0, total;
        var games = new List<GameDetails>();
        progressArgs?.IsIndeterminate = false;

        const int pageSize = 100, maxRequestsPerMinute = 29, maxGamesPerMinute = pageSize * maxRequestsPerMinute;

        var timeConstraint = TimeLimiter.GetFromMaxCountByInterval(maxRequestsPerMinute, TimeSpan.FromMinutes(1));

        do
        {
            Task.Run(async () => await timeConstraint, progressArgs?.CancelToken ?? CancellationToken.None).GetAwaiter().GetResult();

            var searchResult = steamSearch.SearchGames(prop.Param, prop.Value, start, pageSize);
            total = searchResult.TotalCount;

            games.AddRange(steamSearch.ParseSearchResultHtml(searchResult.ResultsHtml));

            start += pageSize;

            if (progressArgs != null)
            {
                var progressText = $"Downloading {prop.Name}… {games.Count}/{total}";
                if (total > maxGamesPerMinute)
                    progressText += $"""

                                     Download will pause every {maxGamesPerMinute} per minute, to wait out rate limiting.
                                     """;

                progressArgs.ProgressMaxValue = searchResult.TotalCount;
                progressArgs.CurrentProgressValue = games.Count;
                progressArgs.Text = progressText;
                _logger.Info(progressText);
                _logger.Info($"Actual downloaded game count: {games.Count}");
            }
        } while (start < total && progressArgs?.CancelToken.IsCancellationRequested != true);

        //deduplicate
        var groupedGames = games.GroupBy(g => g.Url);
        foreach (var gr in groupedGames)
        {
            var gamesInGroup = gr.ToList();
            if (gamesInGroup.Count > 1)
                _logger.Info($"Found {gamesInGroup.Count} games for {gamesInGroup[0]} - {gr.Key}");

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
