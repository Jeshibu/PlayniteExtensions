using PlayniteExtensions.Tests.Common;
using System;
using Xunit;

namespace BigFishMetadata.Tests;

public class ScraperTests
{
    private const string ChimerasHtmlFile = "files/Chimeras.html";

    private static BigFishSearchProvider SetupChimeras(CommunityScoreType scoreType)
    {
        var downloader = new FakeWebDownloader(new()
        {
            { ChimerasHtmlFile, ChimerasHtmlFile },
            { BigFishSearchProvider.GetReviewsUrl("143830"), "files/Chimeras-reviews.json" },
        });
        return new(downloader, new() { CommunityScoreType = scoreType });
    }

    [Fact]
    public void Chimeras()
    {
        var sp = SetupChimeras(CommunityScoreType.StarRating);

        var result = sp.GetDetails(new() { Url = ChimerasHtmlFile });

        Assert.Single(result.Names, "Chimeras: The Lost Film Collector's Edition");
        Assert.Equal(3, result.Genres.Count);
        Assert.Contains("Hidden Object Adventure", result.Genres);
        Assert.Contains("Chimeras", result.Genres);
        Assert.Contains("Mystery", result.Genres);
        Assert.Single(result.Developers, "Elephant");
        Assert.Equal(new(2025, 9, 26), result.ReleaseDate);
        Assert.Equal(948 * Math.Pow(1024, 2), result.InstallSize.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Description));
        Assert.Equal(72, result.CommunityScore);
    }

    [Fact]
    public void ChimerasPercentageRecommended()
    {
        var sp = SetupChimeras(CommunityScoreType.PercentageRecommended);

        var result = sp.GetDetails(new() { Url = ChimerasHtmlFile });

        Assert.Equal(50, result.CommunityScore);
    }
}
