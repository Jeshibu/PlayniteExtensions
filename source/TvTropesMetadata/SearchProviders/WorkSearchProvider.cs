using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Threading;
using TvTropesMetadata.Scraping;
using System.Linq;

namespace TvTropesMetadata.SearchProviders
{
    public class WorkSearchProvider : IGameSearchProvider<TvTropesSearchResult>
    {
        private readonly WorkScraper scraper;
        private readonly TvTropesMetadataSettings settings;

        public WorkSearchProvider(WorkScraper scraper, TvTropesMetadataSettings settings)
        {
            this.scraper = scraper;
            this.settings = settings;
        }

        public GameDetails GetDetails(TvTropesSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
        {
            var result = scraper.GetTropesForGame(searchResult.Url);
            var output = new GameDetails { Description = result.Description, Series = result.Franchises, Url = searchResult.Url };
            output.Names.Add(result.Title);
            output.Tags.AddRange(output.Tags.Select(t => $"{settings.TropePrefix}{t}"));
            if (!string.IsNullOrWhiteSpace(result.CoverImageUrl))
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
