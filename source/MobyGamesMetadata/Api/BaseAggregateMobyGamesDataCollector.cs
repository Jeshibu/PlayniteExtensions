using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;
using System;

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
            result.SetName(mobyGame.Title.HtmlDecode(), mobyGame.AlternateTitles.Select(t => t.Title.HtmlDecode()));
            result.PlatformNames = mobyGame.Platforms.Select(p => p.Name).ToList();
            result.Platforms = mobyGame.Platforms.SelectMany(p => platformUtility.GetPlatforms(p.Name)).ToList();
            result.ReleaseDate = mobyGame.Platforms.Select(p => p.FirstReleaseDate).OrderBy(d => d).FirstOrDefault()?.ParseReleaseDate(logger);
            result.SetUrlAndId(mobyGame.MobyUrl);
            return result;
        }

        protected GameDetails ToGameDetails(MobyGame mobyGame)
        {
            if (mobyGame == null) return null;
            var gameDetails = new GameDetails
            {
                Description = GiantBombMetadata.GiantBombHelper.MakeHtmlUrlsAbsolute(mobyGame.Description, mobyGame.MobyUrl),
            };
            gameDetails.Names.Add(mobyGame.Title);
            if (mobyGame.AlternateTitles != null)
                gameDetails.Names.AddRange(mobyGame.AlternateTitles.Select(t => t.Title));

            if (mobyGame.SampleCover?.Image != null)
                gameDetails.CoverOptions.Add(ToIImageData(mobyGame.SampleCover));

            if (mobyGame.Genres != null)
                foreach (var genre in mobyGame.Genres)
                    AssignGenre(gameDetails, genre);

            gameDetails.Url = mobyGame.MobyUrl;
            if (mobyGame.OfficialUrl != null)
                gameDetails.Links.Add(new Link("Official website", mobyGame.OfficialUrl));

            if (mobyGame.Platforms != null)
            {
                foreach (var platform in mobyGame.Platforms)
                {
                    gameDetails.Platforms.AddRange(platformUtility.GetPlatforms(platform.Name));
                    var releaseDate = platform.FirstReleaseDate.ParseReleaseDate(logger);
                    gameDetails.ReleaseDate = GetEarliestReleaseDate(releaseDate, gameDetails.ReleaseDate);
                }
            }

            if (mobyGame.SampleScreenshots != null)
                gameDetails.BackgroundOptions.AddRange(mobyGame.SampleScreenshots.Select(ToIImageData));

            return gameDetails;
        }

        protected ReleaseDate? GetEarliestReleaseDate(ReleaseDate? r1, ReleaseDate? r2)
        {
            if (r1 == null) return r2;
            if (r2 == null) return r1;
            var dates = new List<ReleaseDate> { r1.Value, r2.Value };
            return dates.OrderBy(d => d.Date).First();
        }

        protected void AssignGenre(GameDetails gameDetails, Genre g)
        {
            var genreSettings = settings.Genres.FirstOrDefault(x => x.Id == g.Id);
            if (genreSettings == null)
            {
                gameDetails.Tags.Add(g.Name);
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
                list.Add(g.Name);
            else
                list.Add(genreSettings.NameOverride);
        }

        protected static BasicImage ToIImageData(MobyImage image)
        {
            return new BasicImage(image.Image)
            {
                Height = image.Height,
                Width = image.Width,
                ThumbnailUrl = image.ThumbnailImage,
            };
        }

        protected static BasicImage ToIImageData(CoverImage image)
        {
            var img = ToIImageData((MobyImage)image);
            img.Platforms = image.Platforms;
            return img;
        }

    }
}
