using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;
using System;
using MobyGamesMetadata.Api.V2;

namespace MobyGamesMetadata.Api
{
    public abstract class BaseAggregateMobyGamesDataCollector
    {
        protected MobyGamesApiClient apiClient;
        protected MobyGamesScraper scraper;
        protected MobyGamesMetadataSettings settings;
        protected IPlatformUtility platformUtility;
        protected ILogger logger = LogManager.GetLogger();

        public BaseAggregateMobyGamesDataCollector(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
        {
            this.apiClient = apiClient;
            this.scraper = scraper;
            this.settings = settings;
            this.platformUtility = platformUtility;
        }

        protected GameDetails Merge(GameDetails scraperDetails, GameDetails apiDetails)
        {
            if (scraperDetails == null) return apiDetails;
            if (apiDetails == null) return scraperDetails;

            apiDetails.CommunityScore = scraperDetails.CommunityScore;
            apiDetails.CriticScore = scraperDetails.CriticScore;
            apiDetails.Tags.AddMissing(scraperDetails.Tags);
            apiDetails.Series.AddMissing(scraperDetails.Series);
            AddCompanies(apiDetails.Developers, scraperDetails.Developers);
            AddCompanies(apiDetails.Publishers, scraperDetails.Publishers);
            if (apiDetails.Description == null)
                apiDetails.Description = scraperDetails.Description;

            return apiDetails;
        }

        protected List<string> AddCompanies(List<string> companies, IEnumerable<string> companiesToAdd)
        {
            if (companiesToAdd == null) return companies;

            companies.AddMissing(companiesToAdd.Select(FixCompanyName));

            return companies;
        }

        protected static string FixCompanyName(string companyName) => companyName.TrimEnd(", the").TrimCompanyForms();

        protected GameSearchResult ToSearchResult(MobyGame mobyGame)
        {
            var result = new GameSearchResult();
            result.SetName(mobyGame.title, mobyGame.highlights);
            result.PlatformNames = mobyGame.platforms.Select(p => p.name).ToList();
            result.Platforms = mobyGame.platforms.SelectMany(p => platformUtility.GetPlatforms(p.name)).ToList();
            result.ReleaseDate = mobyGame.platforms.Select(p => p.release_date).OrderBy(d => d).FirstOrDefault()?.ParseReleaseDate(logger);
            result.SetUrlAndId(mobyGame.moby_url);
            return result;
        }

        protected GameDetails ToGameDetails(MobyGame mobyGame, Game searchGame = null)
        {
            if (mobyGame == null) return null;
            var gameDetails = new GameDetails
            {
                Description = mobyGame.description.MakeHtmlUrlsAbsolute(mobyGame.moby_url),
            };

            gameDetails.Names.Add(mobyGame.title);
            if (mobyGame.highlights != null)
                gameDetails.Names.AddRange(mobyGame.highlights);

            foreach (var genre in mobyGame.genres)
                AssignGenre(gameDetails, genre);

            foreach (var platform in mobyGame.platforms)
            {
                gameDetails.Platforms.AddRange(platformUtility.GetPlatforms(platform.name));
                if (settings.MatchPlatformsForReleaseDate && platformUtility.PlatformsOverlap(searchGame.Platforms, new[] { platform.name }))
                    gameDetails.ReleaseDate = GetEarliestReleaseDate(gameDetails.ReleaseDate, platform.release_date.ParseReleaseDate());
            }

            if (!settings.MatchPlatformsForReleaseDate)
                gameDetails.ReleaseDate = mobyGame.release_date.ParseReleaseDate();

            if (mobyGame.moby_score.HasValue)
                gameDetails.CommunityScore = (int)Math.Round(mobyGame.moby_score.Value * 10);

            foreach (var dev in MatchPlatforms(searchGame, mobyGame.developers, settings.MatchPlatformsForDevelopers))
                gameDetails.Developers.Add(FixCompanyName(dev.name));

            foreach (var pub in MatchPlatforms(searchGame, mobyGame.publishers, settings.MatchPlatformsForPublishers))
                gameDetails.Publishers.Add(FixCompanyName(pub.name));

            //images
            foreach (var covergroup in MatchPlatforms(searchGame, mobyGame.covers, settings.Cover.MatchPlatforms))
                gameDetails.CoverOptions.AddRange(covergroup.images.Select(ToIImageData));

            foreach (var screenshotGroup in MatchPlatforms(searchGame, mobyGame.screenshots, settings.Background.MatchPlatforms))
                gameDetails.BackgroundOptions.AddRange(screenshotGroup.images.Select(ToIImageData));

            //links
            gameDetails.Url = mobyGame.moby_url;
            if (mobyGame.official_url != null)
                gameDetails.Links.Add(new Link("Official website", mobyGame.official_url));

            return gameDetails;
        }

        private IEnumerable<T> MatchPlatforms<T>(Game searchGame, IEnumerable<T> objects, bool onlyMatchingPlatforms) where T : IHasPlatforms
        {
            if (objects == null)
                return new T[0];

            if (!onlyMatchingPlatforms || searchGame?.Platforms == null)
                return objects;

            return objects.Where(o => platformUtility.PlatformsOverlap(searchGame.Platforms, o.Platforms));
        }

        protected ReleaseDate? GetEarliestReleaseDate(ReleaseDate? r1, ReleaseDate? r2)
        {
            if (r1 == null) return r2;
            if (r2 == null) return r1;
            var dates = new List<ReleaseDate> { r1.Value, r2.Value };
            return dates.OrderBy(d => d.Date).First();
        }

        protected void AssignGenre(GameDetails gameDetails, MobyGenre g)
        {
            var genreSettings = settings.Genres.FirstOrDefault(x => x.Id == g.id);
            if (genreSettings == null)
            {
                gameDetails.Tags.Add(g.name);
                return;
            }
            List<string> list;
            switch (genreSettings.ImportTarget)
            {
                case PropertyImportTarget.Genres:
                    list = gameDetails.Genres;
                    break;
                case PropertyImportTarget.Tags:
                    list = gameDetails.Tags;
                    break;
                case PropertyImportTarget.Series:
                    list = gameDetails.Series;
                    break;
                case PropertyImportTarget.Features:
                    list = gameDetails.Features;
                    break;
                case PropertyImportTarget.Ignore:
                default:
                    return;
            }
            if (string.IsNullOrWhiteSpace(genreSettings.NameOverride))
                list.Add(g.name);
            else
                list.Add(genreSettings.NameOverride);
        }

        protected static BasicImage ToIImageData(MobyImage image)
        {
            return new BasicImage(image.image_url)
            {
                Height = image.height,
                Width = image.width,
                ThumbnailUrl = image.thumbnail_url,
            };
        }

        protected static BasicImage ToIImageData(MobyImage image, IEnumerable<string> platforms)
        {
            var output = ToIImageData(image);

            if (platforms != null)
                output.Platforms = platforms;

            return output;
        }
    }
}
