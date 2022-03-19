using System.Collections.Generic;

namespace SteamTagsImporter
{
    public interface ISteamTagScraper
    {
        IEnumerable<string> GetTags(string appId);
    }
}