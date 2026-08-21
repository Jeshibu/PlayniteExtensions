using Playnite.SDK;
using Rawg.Common;
using RawgMetadata.Database;
using System.Collections.Generic;
using System.IO;

namespace RawgMetadata.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var db = new RawgDatabase(Path.GetFullPath("./"));
        db.CreateDatabase(new FakeRawgApi());
        Assert.Equal(2, db.GetTagCount());
    }

    private class FakeRawgApi: IRawgApiClient
    {
        public ICollection<RawgTag> GetTags(GlobalProgressActionArgs a = null) =>
        [
            new() { Id = 1, Language = "eng", Slug = "singleplayer", Name = "Single player", GamesCount = 78934 },
            new() { Id = 2, Language = "eng", Slug = "coop", Name = "Co-op", GamesCount = 235 },
        ];
    }
}
