using LaunchBoxMetadata.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LaunchBoxMetadata;

public static class LaunchBoxHelper
{
    public static string[] SplitLaunchBox(this string str)
    {
        if (str == null)
            return [];

        return str
            .Split([';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
    }

    /// <summary>
    /// This should always be a sequence of characters that is in no game (alternate) name
    /// Obvious things like ; or , are in quite a few game names, thus 4 pipe symbols
    /// </summary>
    public const string AliasSeparator = "||||";

    public static string[] SplitAliases(this string str)
    {
        if (str == null)
            return [];

        return str.Split([AliasSeparator], StringSplitOptions.None);
    }

    public static List<string> GetWhitelistedRegions(List<Region> regions, LaunchBoxMetadataSettings settings)
    {
        var comparer = StringComparer.InvariantCultureIgnoreCase;
        var gameRegions = regions?.Select(r => r.Name).ToList();
        if (settings.PreferGameRegion && gameRegions != null && gameRegions.Any())
        {
            var output = new List<string>();
            foreach (var regionSetting in settings.Regions)
            {
                var aliases = regionSetting.Aliases?.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                if (gameRegions.Any(gr => comparer.Equals(gr, regionSetting.Name) || (aliases != null && aliases.ContainsString(gr))))
                    output.Add(regionSetting.Name); //put any matched region at the top
            }

            foreach (var regionSetting in settings.Regions)
            {
                if (regionSetting.Checked && !output.Contains(regionSetting.Name))
                    output.Add(regionSetting.Name); //add the rest of the enabled regions
            }

            return output;
        }
        else
        {
            return settings.Regions.Where(r => r.Checked).Select(r => r.Name).ToList();
        }
    }

    public static List<LaunchBoxImageDetails> GetImageDetails(LaunchBoxWebScraper scraper, string gameUrl = "", long databaseId = default)
    {
        if (databaseId == default && string.IsNullOrEmpty(gameUrl))
            return [];

        var detailsUrl = string.IsNullOrEmpty(gameUrl) ? scraper.GetLaunchBoxGamesDatabaseUrl(databaseId) : gameUrl;

        if (string.IsNullOrWhiteSpace(detailsUrl))
        {
            LogManager.GetLogger().Error($"Could not retrieve website ID for database ID {databaseId}");
            return [];
        }

        return [.. scraper.GetGameImageDetails(detailsUrl)];
    }

    public static LaunchBoxGame FindGameInBackground(LaunchBoxDatabase database, Game gameData, IPlatformUtility platformUtility)
    {
        var results = database.SearchGames(gameData.Name, 100);
        var deflatedSearchGameName = gameData.Name.Deflate();
        foreach (var game in results)
        {
            var deflatedMatchedGameName = game.MatchedName.Deflate();
            if (!deflatedSearchGameName.Equals(deflatedMatchedGameName, StringComparison.InvariantCultureIgnoreCase))
                continue;

            if (platformUtility.PlatformsOverlap(gameData?.Platforms, game.Platform?.SplitLaunchBox()))
                return game;
            }

        return new LaunchBoxGame();
    }

    public static LaunchBoxGame FindGameViaSearch(LaunchBoxDatabase database, Game gameData)
    {
        var chosen = API.Instance.Dialogs.ChooseItemWithSearch(null, s =>
        {
            var results = database.SearchGames(s).Select(LaunchBoxGameItemOption.FromLaunchBoxGame).ToList<GenericItemOption>();
            return results;
        }, gameData.Name, "LaunchBox: select game");
        if (chosen == null)
            return new LaunchBoxGame();
        else
            return ((LaunchBoxGameItemOption)chosen).Game;
    }

    public static bool FilterImage(LaunchBoxImageDetails imgDetails, ICollection<string> whitelistedTypes, ICollection<string> whitelistedRegions, LaunchBoxImageSourceSettings imgSetting)
    {
        if (!whitelistedTypes.Contains(imgDetails.Type))
            return false;

        if (!whitelistedRegions.Contains(imgDetails.Region))
            return false;

        if (imgDetails.Width < imgSetting.MinWidth || imgDetails.Height < imgSetting.MinHeight)
            return false;

        return imgSetting.AspectRatio switch
        {
            AspectRatio.Vertical => imgDetails.Width < imgDetails.Height,
            AspectRatio.Horizontal => imgDetails.Width > imgDetails.Height,
            AspectRatio.Square => imgDetails.Width == imgDetails.Height,
            _ => true,
        };
    }
}
