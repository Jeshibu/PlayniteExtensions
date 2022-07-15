using System.Collections.Generic;

namespace SteamTagsImporter
{
    public interface ISteamTagScraper
    {
        SteamTagScraper.Delistable<IEnumerable<string>> GetTags(string appId);
    }
}