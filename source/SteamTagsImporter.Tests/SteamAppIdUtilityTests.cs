using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System.IO;
using System.Text;
using Xunit;

namespace SteamTagsImporter.Tests;

public class SteamAppIdUtilityTests
{
    private class FakeCachedFile(string localFilePath, Encoding encoding) : ICachedFile
    {
        public string LocalFilePath { get; } = localFilePath;
        public Encoding Encoding { get; } = encoding;

        public string GetFileContents()
        {
            return File.ReadAllText(LocalFilePath, Encoding);
        }

        public void RefreshCache()
        {
        }
    }

    private static SteamAppIdUtility Setup()
    {
        var scraper = new SteamAppIdUtility(new FakeCachedFile("./applist.json", Encoding.UTF8));
        return scraper;
    }

    [Fact]
    public void NullLinkCollectionDoesNotThrowException()
    {
        var game = new Game("THOR.N");
        var util = Setup();
        var id = util.GetSteamGameId(game);
        Assert.Null(id);
    }

    [Theory]
    [InlineData("Half-Life 2", "220")]
    [InlineData("HalfLife 2", "220")]
    public void GamesCanBeFoundByName(string name, string expectedId)
    {
        var game = new Game(name);
        var util = Setup();
        var id = util.GetSteamGameId(game);
        Assert.Equal(expectedId, id);
    }
}
