using GiantBombMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace GiantBombMetadata.SearchProviders;

public class GiantBombGameSearchProvider(IGiantBombApiClient apiClient, GiantBombMetadataSettings settings, IPlatformUtility platformUtility) : IGameSearchProvider<GiantBombSearchResultItem>
{
    private readonly ILogger logger = LogManager.GetLogger();

    public GameDetails GetDetails(GiantBombSearchResultItem searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        var result = apiClient.GetGameDetails(searchResult.Guid, progressArgs?.CancelToken ?? default);
        if (result == null) return null;
        return ToGameDetails(result);
    }

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        gameDetails = null;
        string guid = game.GetGiantBombGuidFromGameLinks();
        if (guid == null)
            return false;

        var gbDetails = apiClient.GetGameDetails(guid, cancellationToken);
        gameDetails = ToGameDetails(gbDetails);

        return gameDetails != null;
    }

    public IEnumerable<GiantBombSearchResultItem> Search(string query, CancellationToken cancellationToken = default)
    {
        var searchOutput = new List<GiantBombSearchResultItem>();

        if (string.IsNullOrWhiteSpace(query))
            return searchOutput;

        if (Regex.IsMatch(query, @"^3030-[0-9]+$"))
        {
            try
            {
                var gameById = apiClient.GetGameDetails(query, cancellationToken);
                var fakeSearchResult = new GiantBombSearchResultItem()
                {
                    Aliases = gameById.Aliases,
                    ApiDetailUrl = gameById.ApiDetailUrl,
                    Deck = gameById.Deck,
                    Description = gameById.Description,
                    Guid = gameById.Guid,
                    Image = gameById.Image,
                    Id = gameById.Id,
                    Name = gameById.Name,
                    Platforms = gameById.Platforms,
                    ReleaseDate = gameById.ReleaseDate,
                    ResourceType = "game",
                    SiteDetailUrl = gameById.SiteDetailUrl,
                };
                searchOutput.Add(fakeSearchResult);
            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to get Giant Bomb data by ID for <{query}>");
            }
        }

        try
        {
            var searchResult = apiClient.SearchGames(query, cancellationToken);
            searchOutput.AddRange(searchResult);

        }
        catch (Exception e)
        {
            logger.Error(e, $"Failed to get Giant Bomb search data for <{query}>");
            throw;
        }


        var result = apiClient.SearchGames(query, cancellationToken);
        return result;
    }

    public GenericItemOption<GiantBombSearchResultItem> ToGenericItemOption(GiantBombSearchResultItem item)
    {
        var output = new GenericItemOption<GiantBombSearchResultItem>(item);
        output.Name = item.Name;
        if (item.AliasesSplit.Any())
            output.Name += $" (AKA {string.Join("/", item.AliasesSplit)})";

        var description = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(item.ReleaseDate))
            description.Append(item.ReleaseDate);

        if (item.Platforms?.Length > 0)
        {
            if (description.Length > 0)
                description.Append(" | ");

            description.Append(string.Join(", ", item.Platforms.Select(p => p.Abbreviation)));
        }

        if (!string.IsNullOrWhiteSpace(item.Deck))
        {
            if (description.Length > 0)
                description.AppendLine();

            description.Append(item.Deck.HtmlDecode());
        }

        output.Description = description.ToString();

        return output;
    }

    public GameDetails ToGameDetails(GiantBombGameDetails details)
    {
        if (details == null)
            return null;

        var output = new GameDetails();
        output.Names.Add(details.Name.Trim());
        output.Names.AddRange(details.AliasesSplit);
        output.Url = details.SiteDetailUrl;
        output.Description = details.Description.MakeHtmlUrlsAbsolute(details.SiteDetailUrl);
        output.ReleaseDate = details.ReleaseDate.ParseReleaseDate(logger);
        output.Genres.AddRange(GetValues(PropertyImportTarget.Genres, details));
        output.Tags.AddRange(GetValues(PropertyImportTarget.Tags, details));
        if (details.Franchises != null)
        {
            output.Series.AddRange(details.Franchises.Select(f => f.Name.Trim()));

            switch (settings.FranchiseSelectionMode)
            {
                case MultiValuedPropertySelectionMode.All:
                    break;
                case MultiValuedPropertySelectionMode.OnlyShortest:
                    output.Series = output.Series.OrderBy(f => f.Length).ThenBy(f => f).Take(1).ToList();
                    break;
                case MultiValuedPropertySelectionMode.OnlyLongest:
                    output.Series = output.Series.OrderByDescending(f => f.Length).ThenBy(f => f).Take(1).ToList();
                    break;
            }
        }
        if (details.Developers != null)
            output.Developers.AddRange(details.Developers.Select(d => d.Name.Trim()));
        if (details.Publishers != null)
            output.Publishers.AddRange(details.Publishers.Select(p => p.Name.Trim()));
        if (details.Platforms != null)
            output.Platforms.AddRange(details.Platforms.SelectMany(p => platformUtility.GetPlatforms(p.Name.Trim())));
        if (details.Ratings != null)
            output.AgeRatings.AddRange(details.Ratings.Select(r => r.Name.Trim()));
        if (details.Image != null)
            output.CoverOptions.Add(details.Image);
        if (details.Images != null)
            output.BackgroundOptions.AddRange(details.Images.Where(ImageCanBeUsedAsBackground).Where(i => i.Original != details.Image?.Original).Take(4));
        if (details.Genres != null)
            output.Genres.AddRange(details.Genres.Select(g => g.Name.Trim()));

        output.Url = details.SiteDetailUrl;

        return output;
    }

    private static Regex pressEventOrCoverRegex = new(@"\b(e3|pax|blizzcon|box art)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool ImageCanBeUsedAsBackground(GiantBombImage img)
    {
        return img.Tags.Contains("screenshot", StringComparison.InvariantCultureIgnoreCase)
            || img.Tags.Contains("concept", StringComparison.InvariantCultureIgnoreCase)
            || !pressEventOrCoverRegex.IsMatch(img.Tags);
    }

    private ICollection<string> GetValues(PropertyImportTarget target, GiantBombGameDetails data)
    {
        var output = new List<string>();
        output.AddRange(GetValues(settings.Characters, target, data.Characters));
        output.AddRange(GetValues(settings.Concepts, target, data.Concepts));
        output.AddRange(GetValues(settings.Locations, target, data.Locations));
        output.AddRange(GetValues(settings.Objects, target, data.Objects));
        output.AddRange(GetValues(settings.People, target, data.People));
        output.AddRange(GetValues(settings.Themes, target, data.Themes));
        return output.OrderBy(x => x).ToList();
    }

    private IEnumerable<string> GetValues(PropertyImportSetting importSetting, PropertyImportTarget target, GiantBombObject[] data)
    {
        if (importSetting.ImportTarget != target || data == null || data.Length == 0)
            return new string[0];

        return data.Select(d => $"{importSetting.Prefix}{d.Name.Trim()}");
    }
}