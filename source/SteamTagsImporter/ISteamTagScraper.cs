using System.Collections.Generic;

namespace SteamTagsImporter
{
    public interface ISteamTagScraper
    {
        SteamTagScraper.Delistable<IEnumerable<SteamTag>> GetTags(string appId, string languageKey = null);
    }
}