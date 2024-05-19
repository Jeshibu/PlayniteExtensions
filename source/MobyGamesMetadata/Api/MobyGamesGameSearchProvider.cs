using Barnite.Scrapers;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MobyGamesMetadata.Api
{

    public class MobyGamesGameSearchProvider : BaseAggregateMobyGamesDataCollector, IGameSearchProvider<GameSearchResult>
    {
        public MobyGamesGameSearchProvider(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
            : base(apiClient, scraper, settings, platformUtility) { }

        public GameDetails GetDetails(GameSearchResult searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
        {
            return GetDetails(searchResult.Id, searchGame);
        }

        public GameDetails GetDetails(int id, Game searchGame = null)
        {
            if (settings.DataSource == DataSource.ApiAndScraping)
            {
                var scraperDetails = scraper.GetGameDetails(id);
                var apiDetails = apiClient.GetMobyGame(id);
                var output = Merge(scraperDetails, ToGameDetails(apiDetails));
                SetReleaseDetails(output, apiDetails, searchGame);
                return output;
            }
            else if (settings.DataSource.HasFlag(DataSource.Scraping))
            {
                var gameDetails = scraper.GetGameDetails(id);
                gameDetails.Description = GiantBombMetadata.GiantBombHelper.MakeHtmlUrlsAbsolute(gameDetails.Description, gameDetails.Url);
                return gameDetails;
            }
            else if (settings.DataSource.HasFlag(DataSource.Api))
            {
                var apiDetails = apiClient.GetMobyGame(id);
                var output = ToGameDetails(apiDetails);
                SetReleaseDetails(output, apiDetails, searchGame);
                return output;
            }
            return null;
        }

        public IEnumerable<GameSearchResult> Search(string query, CancellationToken cancellationToken = default)
        {
            if (settings.DataSource.HasFlag(DataSource.Scraping))
            {
                return scraper.GetGameSearchResults(query);
            }
            else if (settings.DataSource.HasFlag(DataSource.Api))
            {
                return apiClient.SearchGames(query, cancellationToken).Select(x => ToSearchResult(x));
            }
            return new List<GameSearchResult>();
        }

        public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
        {
            gameDetails = null;
            if (game.Links == null || game.Links.Count == 0)
                return false;

            foreach (var link in game.Links)
            {
                var id = MobyGamesHelper.GetMobyGameIdFromUrl(link.Url);
                if (id == null)
                    continue;

                gameDetails = GetDetails(id.Value, game);

                return gameDetails != null;
            }
            return false;
        }

        public GenericItemOption<GameSearchResult> ToGenericItemOption(GameSearchResult item)
        {
            var output = new GenericItemOption<GameSearchResult>(item)
            {
                Name = item.Name,
                Description = item.Description,
            };
            return output;
        }

        private void SetReleaseDetails(GameDetails output, MobyGame foundGame, Game searchGame = null)
        {
            var matchingPlatforms = foundGame.Platforms.Where(p => platformUtility.PlatformsOverlap(searchGame?.Platforms, new[] { p.Name })).ToList();
            var earliestReleasePlatform = matchingPlatforms.OrderBy(p => p.FirstReleaseDate).FirstOrDefault();

            if (earliestReleasePlatform == null) return;

            var platformResult = apiClient.GetMobyGamePlatform(foundGame.Id, earliestReleasePlatform.Id);

            if (platformResult == null)
            {
                logger.Info($"Empty platform result for Moby Game ID {foundGame.Id}, platform {earliestReleasePlatform.Name}");
                return;
            }

            SetRatings(output, platformResult);

            var release = platformResult.Releases.OrderBy(r => r.ReleaseDate).FirstOrDefault();
            if (release == null) return;

            if (settings.ReleaseDateSource == ReleaseDateSource.EarliestForAutomaticallyMatchedPlatform)
                output.ReleaseDate = release.ReleaseDate.ParseReleaseDate(logger);

            output.Developers = GetCompanyNames(release, "Developed by");
            output.Publishers = GetCompanyNames(release, "Published by");
        }

        private List<string> GetCompanyNames(MobyGameRelease release, params string[] roles)
        {
            return release.Companies
                .Where(c => roles.Contains(c.Role, StringComparer.InvariantCultureIgnoreCase))
                .Select(c => FixCompanyName(c.Name))
                .ToList();
        }

        private void SetRatings(GameDetails output, GamePlatformDetails gamePlatformDetails)
        {
            foreach (var rating in gamePlatformDetails.Ratings)
            {
                var systemName = rating.SystemName.TrimEnd(" Rating");
                if (systemName.EndsWith(")") && systemName.Contains(" ("))
                    systemName = systemName.Substring(0, systemName.IndexOf(" ("));

                if (systemName == "VRCR")
                    output.Tags.Add($"VR Comfort: {rating.Name}");
                else
                    output.AgeRatings.Add($"{systemName} {rating.Name}");
            }
        }
    }
}
