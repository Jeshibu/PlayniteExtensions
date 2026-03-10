using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IgnMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;

namespace IgnMetadata;

public class IgnGameSearchProvider(IgnApiClient client, IPlatformUtility platformUtility) : IGameSearchProvider<IgnGame>
{
    public GameDetails GetDetails(IgnGame searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        var slug = searchResult?.Slug;
        if (slug == null)
            return null;

        var region = searchResult.ObjectRegions.Select(r => r.Region).SkipWhile(string.IsNullOrWhiteSpace).FirstOrDefault()?.ToLowerInvariant();

        var ignDetails = client.Get(searchResult.Slug, region);

        if (ignDetails == null)
            return null;

        var url = $"https://www.ign.com/games/{slug}";

        var gameDetails = new GameDetails
        {
            Url = url,
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
            CriticScore = Get100Score(ignDetails.PrimaryReview?.Score),
            Links = [new("IGN", url)]
        };

        if (ignDetails.PrimaryImage?.Url != null)
            gameDetails.CoverOptions.Add(new BasicImage(ignDetails.PrimaryImage.Url));

        var backgrounds = client.GetImages(searchResult.Slug)?.Select(i => new BasicImage(i));
        if (backgrounds != null)
            gameDetails.BackgroundOptions.AddRange(backgrounds);

        var userReviews = client.GetUserReviewAnalytics(searchResult.Id);
        gameDetails.CommunityScore = Get100Score(userReviews?.Score);

        return gameDetails;
    }

    private static int? Get100Score(double? score)
    {
        if (score == null)
            return null;

        return Convert.ToInt32(score * 10D);
    }

    private static List<string> GetNames(IgnAttribute[] ignAttributes)
    {
        return ignAttributes?.Select(x => x.Name).ToList();
    }

    public IEnumerable<IgnGame> Search(string query, CancellationToken cancellationToken = default) => client.Search(query);

    public GenericItemOption<IgnGame> ToGenericItemOption(IgnGame item)
    {
        var descriptionItems = new List<string>();
        if (item.ReleaseDateString != null)
            descriptionItems.Add(item.ReleaseDateString);

        descriptionItems.AddRange(item.Platforms);

        return new(item) { Name = item.Name, Description = string.Join(" | ", descriptionItems) };
    }

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        gameDetails = null;
        return false;
    }
}
