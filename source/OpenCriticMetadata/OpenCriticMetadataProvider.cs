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

    protected override string ProviderName => "OpenCritic";
}

public class OpenCriticSearchProvider(IPlatformUtility platformUtility, OpenCriticMetadataSettings settings) : IGameSearchProvider<OpenCriticSearchResultItem>
{
    private readonly ILogger _logger = LogManager.GetLogger();

    private readonly RestClient _restClient = new RestClient("https://opencritic-api.p.rapidapi.com/")
                                              .AddDefaultHeader("Content-Type", "application/json")
                                              .AddDefaultHeader("x-rapidapi-host", "opencritic-api.p.rapidapi.com")
                                              .AddDefaultHeader("x-rapidapi-key", settings.ApiKey);

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
        return new(item) { Name = item.Name };
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

        IEnumerable<IImageData> GetSingleImage(OpenCriticImage i)
        {
            if (i?.OG == null)
                yield break;

            yield return i;
        }

        foreach (var option in settings)
            if (option.Checked)
                output.AddRange(option.Name switch
                {
                    Box => GetSingleImage(images.Box),
                    Square => GetSingleImage(images.Square),
                    Masthead => GetSingleImage(images.Masthead),
                    Banner => GetSingleImage(images.Banner),
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
            var response = _restClient.Execute(request);
            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                _logger.Error(response.ErrorMessage);
            }
            var data = JsonConvert.DeserializeObject<T>(response.Content);
            return data;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Error getting {request.Resource}");
            return null;
        }
    }
}
