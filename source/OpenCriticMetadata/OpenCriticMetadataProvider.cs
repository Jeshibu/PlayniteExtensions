using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using static OpenCriticMetadata.ImageTypeNames;

namespace OpenCriticMetadata;

public class OpenCriticMetadataProvider(MetadataRequestOptions options, OpenCriticMetadata plugin, IGameSearchProvider<OpenCriticSearchResultItem> gameSearchProvider, IPlatformUtility platformUtility) : GenericMetadataProvider<OpenCriticSearchResultItem>(gameSearchProvider, options, plugin.PlayniteApi, platformUtility)
{
    public override List<MetadataField> AvailableFields => plugin.SupportedFields;

    protected override string ProviderName { get; } = "OpenCritic";
}

public class OpenCriticSearchProvider(IPlatformUtility platformUtility, OpenCriticMetadataSettings settings) : IGameSearchProvider<OpenCriticSearchResultItem>
{
    private readonly ILogger logger = LogManager.GetLogger();
    private readonly RestClient restClient = new RestClient("https://api.opencritic.com/api/")
        .AddDefaultHeader("Referer", "https://opencritic.com/");

    public GameDetails GetDetails(OpenCriticSearchResultItem searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null) => GetDetails(searchResult.Id);

    private GameDetails GetDetails(int id)
    {
        var gameResult = Execute<OpenCriticGame>(new RestRequest($"game/{id}"));
        var communityResult = Execute<OpenCriticUserRatings>(new RestRequest($"ratings/game/{id}"));
        return ToGameDetails(gameResult, communityResult);
    }

    public IEnumerable<OpenCriticSearchResultItem> Search(string query, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest("meta/search").AddQueryParameter("criteria", query.Replace(' ', '+'));
        var result = Execute<OpenCriticSearchResultItem[]>(request);
        return result?.Where(x => x.Relation == "game");
    }

    public GenericItemOption<OpenCriticSearchResultItem> ToGenericItemOption(OpenCriticSearchResultItem item)
    {
        return new GenericItemOption<OpenCriticSearchResultItem>(item) { Name = item.Name };
    }

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        gameDetails = null;
        return false;
    }

    private GameDetails ToGameDetails(OpenCriticGame game, OpenCriticUserRatings userRatings)
    {
        if (game == null)
            return null;

        var output = new GameDetails
        {
            Platforms = game.Platforms.SelectMany(p => platformUtility.GetPlatforms(p.Name)).ToList(),
            Id = game.Id.ToString(),
            Names = [game.Name],
            Url = game.Url,
        };

        output.CriticScore = settings.CriticScoreSource switch
        {
            OpenCriticSource.TopCritics => GetScore(game.TopCriticScore, game.NumTopCriticReviews, settings.MinimumCriticReviewCount),
            OpenCriticSource.Median => GetScore(game.MedianScore, game.NumReviews, settings.MinimumCriticReviewCount),
            _ => null,
        };
        output.CommunityScore = GetScore(userRatings.Median, userRatings.Count, settings.MinimumCommunityReviewCount);

        if (!string.IsNullOrWhiteSpace(game.Description))
            output.Description = Regex.Replace(game.Description, @"\r?\n", "<br>$0");

        output.Developers.AddRange(GetCompanies(game, "DEVELOPER"));
        output.Publishers.AddRange(GetCompanies(game, "PUBLISHER"));

        if (game.Genres != null)
            output.Genres.AddRange(game.Genres.Select(g => g.Name));

        output.CoverOptions = GetImageOptions(game.Images, settings.CoverSources);
        output.BackgroundOptions = GetImageOptions(game.Images, settings.BackgroundSources);

        if (game.FirstReleaseDate.HasValue)
            output.ReleaseDate = new ReleaseDate(game.FirstReleaseDate.Value.LocalDateTime);

        return output;
    }

    private static List<IImageData> GetImageOptions(OpenCriticImageCollection images, IEnumerable<CheckboxSetting> settings)
    {
        var output = new List<IImageData>();

        if (images == null)
            return output;

        IEnumerable<IImageData> getSingleImage(OpenCriticImage i)
        {
            if (i?.OG == null)
                yield break;

            yield return i;
        }

        foreach (var option in settings)
            if (option.Checked)
                output.AddRange(option.Name switch
                {
                    Box => getSingleImage(images.Box),
                    Square => getSingleImage(images.Square),
                    Masthead => getSingleImage(images.Masthead),
                    Banner => getSingleImage(images.Banner),
                    Screenshots => images.Screenshots ?? [],
                    _ => [],
                });

        return output;
    }

    private static int? GetScore(double? score, int reviewCount, int minimumReviewCount)
    {
        if (score == null || score < 1 || reviewCount < minimumReviewCount)
            return null;

        var output = (int)Math.Round(score.Value);

        return output;
    }

    private static IEnumerable<string> GetCompanies(OpenCriticGame game, string type)
    {
        if (game?.Companies == null)
            return [];

        return game.Companies.Where(c => string.Equals(c.Type, type, StringComparison.InvariantCultureIgnoreCase)).Select(c => c.Name);
    }

    private T Execute<T>(RestRequest request) where T : class
    {
        try
        {
            var response = restClient.Execute(request);
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                logger.Error(response.ErrorMessage);
            }
            var data = JsonConvert.DeserializeObject<T>(response.Content);
            return data;
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error getting {request.Resource}");
            return null;
        }
    }
}

public class OpenCriticBaseModel
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class OpenCriticSearchResultItem : OpenCriticBaseModel, IGameSearchResult
{
    public string Relation { get; set; }

    string IGameSearchResult.Title => Name;

    IEnumerable<string> IGameSearchResult.AlternateNames => [];

    IEnumerable<string> IGameSearchResult.Platforms => [];

    ReleaseDate? IGameSearchResult.ReleaseDate => null;
}

public class OpenCriticGame : OpenCriticBaseModel
{
    public bool? HasLootboxes { get; set; }
    public bool IsMajorRelease { get; set; }
    public OpenCriticImageCollection Images { get; set; }
    public int NumReviews { get; set; }
    public int NumTopCriticReviews { get; set; }
    public double MedianScore { get; set; }
    public double TopCriticScore { get; set; }
    public double Percentile { get; set; }
    public double PercentRecommended { get; set; }
    public string Description { get; set; }
    public DateTimeOffset? FirstReleaseDate { get; set; }
    public List<OpenCriticCompany> Companies { get; set; } = [];
    public List<OpenCriticBaseModel> Genres { get; set; } = [];
    public List<OpenCriticPlatform> Platforms { get; set; } = [];
    public string Url { get; set; }
}

public class OpenCriticImageCollection
{
    public OpenCriticImage Box { get; set; }
    public OpenCriticImage Square { get; set; }
    public OpenCriticImage Masthead { get; set; }
    public OpenCriticImage Banner { get; set; }
    public List<OpenCriticImage> Screenshots { get; set; } = [];
}

public class OpenCriticImage : IImageData
{
    public string OG { get; set; }
    public string SM { get; set; }

    string IImageData.Url => AddDomain(OG);

    string IImageData.ThumbnailUrl => AddDomain(SM);

    int IImageData.Width => 0;

    int IImageData.Height => 0;

    IEnumerable<string> IImageData.Platforms => [];

    private static string AddDomain(string path) => path == null ? null : $"https://img.opencritic.com/{path}";
}

public class OpenCriticCompany
{
    public string Name { get; set; }
    public string Type { get; set; }
}

public class OpenCriticPlatform : OpenCriticBaseModel
{
    public string ShortName { get; set; }
    public DateTimeOffset ReleaseDate { get; set; }
}

public class OpenCriticUserRatings
{
    public double? Median { get; set; }
    public int Count { get; set; }
}
