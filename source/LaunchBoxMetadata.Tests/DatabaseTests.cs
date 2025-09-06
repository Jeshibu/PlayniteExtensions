using System.IO;
using System.Linq;
using Xunit;

namespace LaunchBoxMetadata.Tests;

public class DatabaseTests
{
    public static LaunchBoxDatabase Setup()
    {
        var dir = Path.GetTempPath();
        var db = new LaunchBoxDatabase(dir);
        db.CreateDatabase(new LaunchBoxXmlParser(@"Metadata.xml"));
        return db;
    }

    [Fact]
    public void ReturnsSearchResults()
    {
        var db = Setup();
        var searchResult = db.SearchGames("alien", 50).ToList();
        Assert.Equal(6, searchResult.Count);
    }

    [Fact]
    public void DeduplicatesNames()
    {
        var db = Setup();
        var searchResult = db.SearchGames("lylat wars", 50).ToList();
        Assert.Single(searchResult);
    }

    [Fact]
    public void CanGetGamesByGenre()
    {
        var db = Setup();
        var genres = db.GetGenres().ToList();

        var expectedGenres = new Genre[]
        {
            new() { Name = "Action", Count = 1 },
            new() { Name = "Adventure", Count = 1 },
            new() { Name = "Horror", Count = 2 },
            new() { Name = "Sandbox", Count = 1 },
            new() { Name = "Shooter", Count = 7 },
            new() { Name = "Stealth", Count = 1 },
        };

        Assert.Equal(expectedGenres.Length, genres.Count);
        foreach (var expectedGenre in expectedGenres)
        {
            var genre = genres.SingleOrDefault(g => g.Name == expectedGenre.Name);
            if (genre == null)
                Assert.Fail($"Genre not found: {expectedGenre.Name}");

            if (expectedGenre.Count != genre.Count)
                Assert.Fail($"Genre {expectedGenre.Name} count mismatch: {genre.Count} instead of {expectedGenre.Count}");

            var games = db.GetGamesForGenre(genre.Id).ToList();
            Assert.Equal(genre.Count, games.Count);
        }
    }
}
