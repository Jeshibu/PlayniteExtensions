using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IgnMetadata
{
    public class IgnGameSearchProvider : IGameSearchProvider<IgnGame>
    {
        private IgnClient client;
        private readonly IPlatformUtility platformUtility;

        public IgnGameSearchProvider(IgnClient client, IPlatformUtility platformUtility)
        {
            this.client = client;
            this.platformUtility = platformUtility;
        }

        public GameDetails GetDetails(IgnGame searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
        {
            var slug = searchResult?.Slug;
            if (slug == null)
                return null;

            var region = searchResult.ObjectRegions.Select(r => r.Region).SkipWhile(string.IsNullOrWhiteSpace).FirstOrDefault()?.ToLowerInvariant();

            var ignDetails = client.Get(searchResult.Slug, region);

            if (ignDetails == null)
                return null;

            var gameDetails = new GameDetails
            {
                Names = ignDetails.Names,
                Developers = ignDetails.Producers?.Select(x => x.Name.TrimCompanyForms()).ToList(),
                Publishers = ignDetails.Publishers?.Select(x => x.Name.TrimCompanyForms()).ToList(),
                Genres = GetNames(ignDetails.Genres),
                Features = GetNames(ignDetails.Features),
                Description = ignDetails.Metadata?.Descriptions?.Long ?? ignDetails.Metadata?.Descriptions?.Short,
                Series = GetNames(ignDetails.Franchises),
                AgeRatings = ignDetails.AgeRatings.ToList(),
                Platforms = ignDetails.Platforms.SelectMany(platformUtility.GetPlatforms).ToList(),
                ReleaseDate = ignDetails.ReleaseDate,
            };

            if (ignDetails?.PrimaryImage?.Url != null)
                gameDetails.CoverOptions.Add(new BasicImage(ignDetails.PrimaryImage.Url));

            return gameDetails;
        }

        private static List<string> GetNames(IgnAttribute[] ignAttributes)
        {
            return ignAttributes?.Select(x => x.Name).ToList();
        }

        public IEnumerable<IgnGame> Search(string query, CancellationToken cancellationToken = default) => client.Search(query);

        public GenericItemOption<IgnGame> ToGenericItemOption(IgnGame item)
        {
            var description = item.ReleaseDateString ?? string.Empty;
            var platforms = item.Platforms;
            if (platforms != null)
                foreach (var platform in platforms)
                {
                    if (description.Length > 0)
                        description += " | ";
                    description += platform;
                }
            return new GenericItemOption<IgnGame>(item) { Name = item.Name, Description = description };
        }

        public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
        {
            gameDetails = null;
            return false;
        }
    }
}