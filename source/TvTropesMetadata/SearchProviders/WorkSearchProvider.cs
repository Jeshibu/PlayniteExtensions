using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using TvTropesMetadata.Scraping;
using System.Linq;

namespace TvTropesMetadata.SearchProviders;

public class WorkSearchProvider(WorkScraper scraper, TvTropesMetadataSettings settings) : IGameSearchProvider<TvTropesSearchResult>
{
    private ILogger _logger = LogManager.GetLogger();

    public GameDetails GetDetails(TvTropesSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        try
        {
            var result = scraper.GetTropesForGame(searchResult.Url);
            var output = new GameDetails { Description = result.Description, Series = result.Franchises, Url = searchResult.Url };
            output.Names.Add(result.Title);
            output.Tags.AddRange(result.Tropes.Select(t => $"{settings.TropePrefix}{t}"));
            output.CoverOptions.AddRange(result.CoverImageUrls.Select(ci => new ImgData { Url = ci }));
            return output;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error scraping TV Tropes work page");
            return new();
        }
    }

    private class ImgData : IImageData
    {
        public string Url { get; set; }

        public string ThumbnailUrl { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string Description { get; }

        public IEnumerable<string> Platforms { get; set; } = new List<string>();
    }

    public IEnumerable<TvTropesSearchResult> Search(string query, CancellationToken cancellationToken = default)
    {
        return scraper.Search(query);
    }

    public GenericItemOption<TvTropesSearchResult> ToGenericItemOption(TvTropesSearchResult item) => item.ToGenericItemOption();

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        gameDetails = null;
        return false;
    }
}
