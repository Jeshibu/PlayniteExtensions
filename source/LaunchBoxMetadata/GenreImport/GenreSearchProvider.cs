using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;

namespace LaunchBoxMetadata.GenreImport;

public class GenreSearchProvider(LaunchBoxDatabase database, IPlatformUtility platformUtility) : IBulkPropertyImportDataSource<Genre>
{
    private List<Genre> genres = database.GetGenres().ToList();

    public IEnumerable<Genre> Search(string query, CancellationToken cancellationToken = default)
    {
        return genres.Where(g => g.Name.Contains(query, StringComparison.InvariantCultureIgnoreCase));
    }

    public GenericItemOption<Genre> ToGenericItemOption(Genre item)
    {
        return new(item) { Name = item.Name, Description = $"{item.Count} games in LaunchBox database" };
    }

    public IEnumerable<GameDetails> GetDetails(Genre searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        return database.GetGamesForGenre(searchResult.Id).Select(ToGameDetails).ToList();
    }

    private GameDetails ToGameDetails(LaunchBoxGame game)
    {
        var output = new GameDetails
        {
            Id = "",
            Platforms = game.Platform.Split([';'], StringSplitOptions.RemoveEmptyEntries).SelectMany(platformUtility.GetPlatforms).ToList(),
            Names = [game.Name, ..game.Aliases.SplitAliases()],
        };

        if (game.ReleaseDate.HasValue)
            output.ReleaseDate = new(game.ReleaseDate.Value);

        if (!string.IsNullOrWhiteSpace(game.WikipediaURL))
            output.Links.Add(new("Wikipedia", game.WikipediaURL));

        if (!string.IsNullOrWhiteSpace(game.VideoURL))
            output.Links.Add(new("Video", game.VideoURL));

        return output;
    }
}
