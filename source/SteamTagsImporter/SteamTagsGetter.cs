using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Linq;

namespace SteamTagsImporter;

public class SteamTagsGetter
{
    public SteamTagsGetter(SteamTagsImporterSettings settings, ISteamAppIdUtility appIdUtility, ISteamTagScraper tagScraper)
    {
        this.Settings = settings;
        this.appIdUtility = appIdUtility;
        this.tagScraper = tagScraper;
    }

    public SteamTagsImporterSettings Settings { get; }
    private ISteamAppIdUtility appIdUtility { get; }
    private ISteamTagScraper tagScraper { get; }
    private ILogger logger = LogManager.GetLogger();

    public IEnumerable<SteamTag> GetSteamTags(Game game, out bool newTagsAddedToSettings)
    {
        newTagsAddedToSettings = false;
        if (Settings.LimitTaggingToPcGames && !IsPcGame(game))
        {
            logger.Debug($"Skipped {game.Name} because it's not a PC game");
            return new SteamTag[0];
        }

        string appId = appIdUtility.GetSteamGameId(game);
        if (string.IsNullOrEmpty(appId))
        {
            logger.Debug($"Couldn't find app ID for game {game.Name}");
            return new SteamTag[0];
        }

        var tagScrapeResult = tagScraper.GetTags(appId, Settings.LanguageKey);

        var tags = tagScrapeResult.Value.ToList();

        if (Settings.LimitTagsToFixedAmount)
            tags = tags.Take(Settings.FixedTagCount).ToList();

        tags.RemoveAll(tag => Settings.BlacklistedTags.Contains(tag.Name));

        foreach (var tag in tags)
        {
            newTagsAddedToSettings |= Settings.OkayTags.AddMissing(tag.Name);
        }

        if (tagScrapeResult.Delisted && Settings.TagDelistedGames)
            tags.Add(new SteamTag { TagId = -1, Name = "Delisted" });

        return tags;
    }

    public string GetFinalTagName(string tagName)
    {
        string computedTagName = Settings.UseTagPrefix ? $"{Settings.TagPrefix}{tagName}" : tagName;
        return computedTagName;
    }

    private static bool IsPcGame(Game game)
    {
        var platforms = game.Platforms;
        if (platforms == null || platforms.Count == 0)
            return true; //assume games are for PC if not specified

        foreach (var platform in platforms)
        {
            if (platform.SpecificationId != null && platform.SpecificationId.StartsWith("pc_"))
                return true;
        }

        return false;
    }
}