using PlayniteExtensions.Tests.Common;
using System;
using System.Linq;
using Xunit;

namespace BigFishMetadata.Tests;

public class ScraperTests
{
    private const string GameName = "Chimeras: The Lost Film Collector's Edition";
    private const string ChimerasUrlKey = "chimeras-the-lost-film-ce-f19003t1l1";
    private const int ChimerasProductId = 143830;

    private static BigFishSearchProvider SetupChimeras(CommunityScoreType scoreType)
    {
        var downloader = new FakeWebDownloader(new()
        {
            { BigFishGraphQLService.GetSearchUrl(GameName, BigFishLanguage.English), "files/Chimeras-search.json" },
            { BigFishGraphQLService.GetDetailsUrl(ChimerasUrlKey), "files/Chimeras-details.json" },
            { BigFishGraphQLService.GetReviewsUrl(ChimerasProductId), "files/Chimeras-reviews.json" },
        });
        return new(downloader, new() { CommunityScoreType = scoreType });
    }

    [Fact]
    public void Chimeras()
    {
        var sp = SetupChimeras(CommunityScoreType.StarRating);

        var searchResults = sp.Search(GameName).ToList();

        Assert.NotEmpty(searchResults);

        var result = sp.GetDetails(searchResults.First());

        Assert.Single(result.Names, "Chimeras: The Lost Film Collector's Edition");
        Assert.Contains("Hidden Object Adventure", result.Genres);
        Assert.Contains("Mystery", result.Genres);
        Assert.Equal(2, result.Genres.Count);
        Assert.Single(result.Series, "Chimeras");
        Assert.Single(result.Developers, "Elephant Games");
        Assert.Equal(new(2025, 9, 26), result.ReleaseDate);
        Assert.NotNull(result.InstallSize);
        Assert.Equal(948 * Math.Pow(1024, 2), result.InstallSize.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Description));
        Assert.Equal(75, result.CommunityScore);
        Assert.Equal("https://www.bigfishgames.com/chimeras-the-lost-film-ce-f19003t1l1.html", result.Url);
        Assert.Single(result.Links);
        Assert.Single(result.IconOptions);
        Assert.Single(result.CoverOptions);
        Assert.Equal(3, result.BackgroundOptions.Count);
    }

    [Fact]
    public void ChimerasPercentageRecommended()
    {
        var sp = SetupChimeras(CommunityScoreType.PercentageRecommended);

        var searchResults = sp.Search(GameName).ToList();

        Assert.NotEmpty(searchResults);

        var result = sp.GetDetails(searchResults.First());

        Assert.Equal(55, result.CommunityScore);
    }
}
