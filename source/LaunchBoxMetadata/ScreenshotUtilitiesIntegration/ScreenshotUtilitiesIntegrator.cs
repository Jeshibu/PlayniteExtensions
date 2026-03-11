using KNARZhelper;
using KNARZhelper.MetadataCommon;
using KNARZhelper.ScreenshotsCommon;
using KNARZhelper.ScreenshotsCommon.Models;
using LaunchBoxMetadata.Models;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaunchBoxMetadata.ScreenshotUtilitiesIntegration;

public class ScreenshotUtilitiesIntegrator(LaunchBoxMetadata plugin, LaunchBoxMetadataSettings settings, LaunchBoxWebScraper scraper, IPlatformUtility platformUtility)
{
    private string GetDatabaseUrl(Game game, ScreenshotGroup screenshotGroup, string databaseId = default)
    {
        string searchUrl = default;

        if (databaseId != default && long.TryParse(databaseId, out var dbIdLong))
            searchUrl = scraper.GetLaunchBoxGamesDatabaseUrl(dbIdLong);

        if (searchUrl == default)
            searchUrl = screenshotGroup.GameIdentifier;

        if (searchUrl == default)
        {
            var link = MetadataHelper.GetLink(game, new System.Text.RegularExpressions.Regex(@"gamesdb\.launchbox-app\.com\/games\/details\/"));

            if (link != null)
                searchUrl = link.Url;
        }

        if (searchUrl == default)
        {
            var foundGame = LaunchBoxHelper.FindGameInBackground(new LaunchBoxDatabase(plugin.GetPluginUserDataPath()), game, platformUtility);

            if (foundGame.DatabaseID != default)
            {
                searchUrl = scraper.GetLaunchBoxGamesDatabaseUrl(foundGame.DatabaseID);
            }
        }

        return searchUrl;
    }

    public async Task<bool> LoadScreenshotsFromSourceAsync(Game game, ScreenshotGroup screenshotGroup)
    {
        var url = screenshotGroup.GameIdentifier;

        var updated = false;

        try
        {
            var whitelistedImgTypes = settings.ScreenshotUtilities.ImageTypes.Where(t => t.Checked).Select(t => t.Name).ToList();
            var whitelistedRegions = LaunchBoxHelper.GetWhitelistedRegions(game?.Regions, settings);

            var imageDetails = LaunchBoxHelper.GetImageDetails(scraper, url).Where(i => LaunchBoxHelper.FilterImage(i, whitelistedImgTypes, whitelistedRegions, settings.ScreenshotUtilities)).ToList();

            if (imageDetails == null || !imageDetails.Any())
                return false;

            var filteredimageDetails = imageDetails
                .Where(i => !screenshotGroup.Screenshots.Any(es => es.Path.Equals(i.Url)))
                .OrderBy(i => whitelistedImgTypes.IndexOf(i.Type))
                .ThenBy(i => whitelistedRegions.IndexOf(i.Region))
                .ToList();

            foreach (var image in filteredimageDetails)
            {
                var mediaType = image.Type.Contains("Screenshot") ? MediaType.Screenshot
                    : image.Type.Contains("Advertisement") ? MediaType.Advertisement
                    : image.Type.Contains("Background") ? MediaType.Background
                    : image.Type.Contains("Banner") ? MediaType.Banner
                    : image.Type.Contains("Box - Front") ? MediaType.BoxFront
                    : image.Type.Contains("Box - Back") ? MediaType.BoxBack
                    : image.Type.Contains("Box - Spine") ? MediaType.BoxSpine
                    : image.Type.Contains("Box - 3D") ? MediaType.Box3D
                    : image.Type.Contains("Logo") ? MediaType.Logo
                    : image.Type.Contains("Disc") ? MediaType.Disc
                    : image.Type.Contains("Cart - ") ? MediaType.Cartridge
                    : image.Type.Contains("Arcade - Cabinet") ? MediaType.ArcadeCabinet
                    : image.Type.Contains("Arcade - Circuit Board") ? MediaType.ArcadeCircuit
                    : image.Type.Contains("Arcade - Control Panel") ? MediaType.ArcadeControlPanel
                    : image.Type.Contains("Arcade - Controls Information") ? MediaType.ArcadeControlsInfo
                    : image.Type.Contains("Arcade - Marquee") ? MediaType.ArcadeMarquee
                    : image.Type.Contains("Icon") ? MediaType.Icon
                    : image.Type.Contains("Poster") ? MediaType.Poster
                    : MediaType.Unknown;

                screenshotGroup.Screenshots.Add(new Screenshot(image.Url)
                {
                    ThumbnailPath = image.ThumbnailUrl,
                    Name = image.Type,
                    SortOrder = filteredimageDetails.IndexOf(image),
                    Type = mediaType
                });
            }

            updated = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }

        return updated;
    }

    public async Task<bool> FetchScreenshotsAsync(Game game, int daysSinceLastUpdate, bool forceUpdate, string databaseId = default)
    {
        try
        {
            // return when the main addon isn't installed.
            if (!ScreenshotHelper.IsScreenshotUtilitiesInstalled || game == null)
                return false;

            // load the file.
            (var fileExists, var screenshotGroup) = ScreenshotHelper.LoadGroup(game, plugin.ProviderName, plugin.Id);

            // return if we don't want to force an update and the last update was inside the days configured.
            if (!forceUpdate && (screenshotGroup.LastUpdate > DateTime.Now.AddDays(daysSinceLastUpdate * -1)))
                return false;

            // Get the identifying url to search for.
            var searchUrl = GetDatabaseUrl(game, screenshotGroup, databaseId);

            if (string.IsNullOrEmpty(searchUrl))
                return false;

            // Return if a game was searched and it's the one we already have.
            if (searchUrl != default && searchUrl.Equals(screenshotGroup.GameIdentifier))
                return false;

            // We need to reset the file if we got a new id from the method call and it's not the
            // same we already got.
            if (!fileExists || (databaseId != default && !searchUrl.Equals(screenshotGroup.GameIdentifier)))
            {
                screenshotGroup.GameIdentifier = searchUrl;

                screenshotGroup.Screenshots.Clear();
            }

            var updated = await LoadScreenshotsFromSourceAsync(game, screenshotGroup);

            ScreenshotHelper.SaveScreenshotGroupJson(game, screenshotGroup);

            return updated;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error fetching screenshots for {game.Name}");
            return false;
        }
    }

    public string GetScreenshotSearchResult(Game game, string searchTerm)
    {
        try
        {
            var results = new LaunchBoxDatabase(plugin.GetPluginUserDataPath()).SearchGames(searchTerm).Select(LaunchBoxGameItemOption.FromLaunchBoxGame).ToList();

            if (!results?.Any() ?? true)
                return null;

            var result = new List<ScreenshotSearchResult>(results.Select(item => new ScreenshotSearchResult
            {
                Name = item.Name,
                Description = item.Description,
                Identifier = item.Game.DatabaseID.ToString()
            }));

            return Serialization.ToJson(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error loading data for game {game.Name}");
        }

        return null;
    }
}
