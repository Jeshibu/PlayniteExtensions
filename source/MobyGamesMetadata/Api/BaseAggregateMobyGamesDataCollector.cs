using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;
using System;
using MobyGamesMetadata.Api.V2;

namespace MobyGamesMetadata.Api;

public abstract class BaseAggregateMobyGamesDataCollector(MobyGamesApiClient apiClient, MobyGamesScraper scraper, MobyGamesMetadataSettings settings, IPlatformUtility platformUtility)
{
    protected readonly MobyGamesApiClient ApiClient = apiClient;
    protected readonly MobyGamesScraper Scraper = scraper;
    protected readonly MobyGamesMetadataSettings Settings = settings;
    protected readonly IPlatformUtility PlatformUtility = platformUtility;
    protected ILogger Logger = LogManager.GetLogger();

    protected GameDetails Merge(GameDetails scraperDetails, GameDetails apiDetails)
    {
        if (scraperDetails == null) return apiDetails;
        if (apiDetails == null) return scraperDetails;

        apiDetails.CommunityScore = scraperDetails.CommunityScore;
        apiDetails.CriticScore = scraperDetails.CriticScore;
        apiDetails.Tags.AddMissing(scraperDetails.Tags);
        apiDetails.Series.AddMissing(scraperDetails.Series);
        MergeCompanies(apiDetails.Developers, scraperDetails.Developers);
        MergeCompanies(apiDetails.Publishers, scraperDetails.Publishers);
        apiDetails.Description ??= scraperDetails.Description;
        MergeLinks(apiDetails.Links, scraperDetails.Links);

        return apiDetails;
    }

    private static void MergeLinks(List<Link> apiLinks, List<Link> scraperLinks)
    {
        if (scraperLinks == null) return;

        apiLinks?.AddRange(scraperLinks.Where(sl => apiLinks.All(al => sl.Url != al.Url)));
    }

    private static void MergeCompanies(List<string> companies, IEnumerable<string> companiesToAdd)
    {
        if (companiesToAdd == null) return;

        companies?.AddMissing(companiesToAdd.Select(FixCompanyName));
    }

    private static string FixCompanyName(string companyName) => companyName.TrimEnd(", the").TrimCompanyForms();

    protected static GameSearchResult ToSearchResult(MobyGame mobyGame) => new(mobyGame);

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
            gameDetails.Platforms.AddRange(PlatformUtility.GetPlatforms(platform.name));
            if (Settings.MatchPlatformsForReleaseDate && PlatformUtility.PlatformsOverlap(searchGame?.Platforms, [platform.name]))
                gameDetails.ReleaseDate = GetEarliestReleaseDate(gameDetails.ReleaseDate, platform.release_date.ParseReleaseDate());
        }

        if (!Settings.MatchPlatformsForReleaseDate)
            gameDetails.ReleaseDate = mobyGame.release_date.ParseReleaseDate();

        if (mobyGame.moby_score.HasValue)
            gameDetails.CommunityScore = (int)Math.Round(mobyGame.moby_score.Value * 10);

        foreach (var dev in MatchPlatforms(searchGame, mobyGame.developers, Settings.MatchPlatformsForDevelopers))
            gameDetails.Developers.Add(FixCompanyName(dev.name));

        foreach (var pub in MatchPlatforms(searchGame, mobyGame.publishers, Settings.MatchPlatformsForPublishers))
            gameDetails.Publishers.Add(FixCompanyName(pub.name));

        //images
        foreach (var coverGroup in MatchPlatforms(searchGame, mobyGame.covers, Settings.Cover.MatchPlatforms))
            gameDetails.CoverOptions.AddRange(coverGroup.images.Select(ToIImageData));

        foreach (var screenshotGroup in MatchPlatforms(searchGame, mobyGame.screenshots, Settings.Background.MatchPlatforms))
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
            return [];

        if (!onlyMatchingPlatforms || searchGame?.Platforms == null)
            return objects;

        return objects.Where(o => PlatformUtility.PlatformsOverlap(searchGame.Platforms, o.Platforms));
    }

    private static ReleaseDate? GetEarliestReleaseDate(ReleaseDate? r1, ReleaseDate? r2)
    {
        if (r1 == null) return r2;
        if (r2 == null) return r1;
        var dates = new List<ReleaseDate> { r1.Value, r2.Value };
        return dates.OrderBy(d => d.Date).First();
    }

    private void AssignGenre(GameDetails gameDetails, MobyGenre g)
    {
        var genreSettings = Settings.Genres.FirstOrDefault(x => x.Id == g.id);
        if (genreSettings == null)
        {
            gameDetails.Tags.Add(g.name);
            return;
        }

        var list = GetGenreImportTarget(genreSettings, gameDetails);

        if (string.IsNullOrWhiteSpace(genreSettings.NameOverride))
            list?.Add(g.name);
        else
            list?.Add(genreSettings.NameOverride);
    }

    private static List<string> GetGenreImportTarget(MobyGamesGenreSetting genreSetting, GameDetails gameDetails) => genreSetting?.ImportTarget switch
    {
        PropertyImportTarget.Genres => gameDetails.Genres,
        PropertyImportTarget.Tags => gameDetails.Tags,
        PropertyImportTarget.Series => gameDetails.Series,
        PropertyImportTarget.Features => gameDetails.Features,
        _ => null,
    };

    private static BasicImage ToIImageData(MobyImage image) => new(image.image_url)
    {
        Height = image.height,
        Width = image.width,
        ThumbnailUrl = image.thumbnail_url,
    };
}
