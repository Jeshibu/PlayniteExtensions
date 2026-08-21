using Playnite.SDK;
using RawgMetadata.Database;
using RestSharp;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Rawg.Common;

public partial class RawgApiClient: IRawgApiClient
{
    public ICollection<RawgTag> GetTags(GlobalProgressActionArgs a = null)
    {
        var request = new RestRequest("tags")
                      .AddParameter("page", 1)
                      .AddParameter("page_size", 40)
                      .AddKey(Key);

        return GetAllPages<RawgTag>(request, a, "Downloading RAWG tags…");
    }

    public ICollection<RawgGameDetails> GetGamesByTag(RawgTag tag, GlobalProgressActionArgs a = null)
    {
        var request = new RestRequest("games")
                      .AddParameter("tags", tag.Slug)
                      .AddParameter("ordering", "created")
                      .AddParameter("page", 1)
                      .AddParameter("page_size", 40)
                      .AddKey(Key);

        return GetAllPages<RawgGameDetails>(request, a, $"Downloading RAWG games for tag [{tag.Name}]…");
    }
}

public interface IRawgApiClient
{
    ICollection<RawgTag> GetTags(GlobalProgressActionArgs a = null);
}
