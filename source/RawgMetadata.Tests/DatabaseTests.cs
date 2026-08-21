using Playnite.SDK;
using Rawg.Common;
using RawgMetadata.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RawgMetadata.Tests;

public class DatabaseTests(DatabaseFixture fixture): IClassFixture<DatabaseFixture>
{
    [Fact]
    public void AllTagsAreImported()
    {
        Assert.Equal(2, fixture.Database.GetTagCount());
    }

    [Fact]
    public void SearchTags()
    {
        var searchResult = fixture.Database.SearchTags("sing").ToList();
        Assert.Single(searchResult);
        Assert.Equal(123, searchResult[0].Id);
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class DatabaseFixture : IDisposable
{
    public RawgDatabase Database { get; }

    public DatabaseFixture()
    {
        Database = new RawgDatabase(Path.GetTempPath());
        Database.CreateDatabase(new FakeRawgApi());
    }

    public void Dispose() => Database.DeleteDatabase();

    private class FakeRawgApi: IRawgApiClient
    {
        public ICollection<RawgTag> GetTags(GlobalProgressActionArgs a = null) =>
        [
            new() { Id = 123, Language = "eng", Slug = "singleplayer", Name = "Single player", GamesCount = 78934 },
            new() { Id = 234, Language = "eng", Slug = "coop", Name = "Co-op", GamesCount = 235 },
        ];
    }
}
