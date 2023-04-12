using Barnite.Scrapers;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;

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

            scraperDetails.CoverOptions = apiDetails.CoverOptions;
            scraperDetails.BackgroundOptions = apiDetails.BackgroundOptions;
            scraperDetails.Links = apiDetails.Links;

            if (apiDetails.Description != null)
                scraperDetails.Description = apiDetails.Description;

            if (apiDetails.Genres != null)
                scraperDetails.Genres = apiDetails.Genres;

            if (apiDetails.Tags != null)
            {
                scraperDetails.Tags.AddRange(apiDetails.Tags);
                scraperDetails.Tags = scraperDetails.Tags.Distinct().ToList();
            }
            return scraperDetails;
        }

        protected GameSearchResult ToSearchResult(MobyGame mobyGame)
        {
            var result = new GameSearchResult();
            result.SetName(mobyGame.Title, mobyGame.AlternateTitles.Select(t => t.Title));
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
                Description = mobyGame.Description,
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
            gameDetails.Links.Add(new Link("MobyGames", mobyGame.MobyUrl));
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
            var list = GetRelevantList(gameDetails, g);
            if (list == null) return;
            list.Add(g.Name);
        }

        protected List<string> GetRelevantList(GameDetails gameDetails, Genre g)
        {
            switch (g.Category)
            {
                case "Basic Genres":
                case "Perspective":
                case "Gameplay":
                case "Narrative Theme/Topic":
                    return gameDetails.Genres;
                case "Visual Presentation":
                case "Art Style":
                case "Pacing":
                case "Interface/Control":
                case "Sports Themes":
                case "Educational Categories":
                case "Vehicular Themes":
                case "Setting":
                case "Special Edition":
                case "Other Attributes":
                    return gameDetails.Tags;
                case "DLC/Add-on":
                default:
                    return null;
            }
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
