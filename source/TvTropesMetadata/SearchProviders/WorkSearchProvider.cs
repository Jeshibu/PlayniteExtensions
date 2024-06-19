using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Threading;
using TvTropesMetadata.Scraping;
using PlayniteExtensions.Common;

namespace TvTropesMetadata.SearchProviders
{
    public class WorkSearchProvider : IGameSearchProvider<TvTropesSearchResult>
    {
        private readonly WorkScraper scraper;

        public WorkSearchProvider(WorkScraper scraper)
        {
            this.scraper = scraper;
        }

        public GameDetails GetDetails(TvTropesSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
        {
            var result = scraper.GetTropesForGame(searchResult.Url);
            var output = new GameDetails { Description = result.Description, Tags = result.Tropes, Series = result.Franchises, Url = searchResult.Url };
            output.Names.Add(result.Title);
            if(!string.IsNullOrWhiteSpace(result.CoverImageUrl))
                output.CoverOptions.Add(new ImgData { Url = result.CoverImageUrl });
            return output;
        }

        private class ImgData : IImageData
        {
            public string Url { get; set; }

            public string ThumbnailUrl { get; set; }

            public int Width { get; set; }

            public int Height { get; set; }

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
}
