using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rawg.Common;

public static partial class RawgMetadataHelper
{
    private static readonly Regex yearRegex = new(@" \([0-9]{4}\)$", RegexOptions.Compiled);

    public static string StripYear(string gameName)
    {
        return yearRegex.Replace(gameName, string.Empty);
    }

    public static string NormalizeNameForComparison(string gameName)
    {
        return StripYear(gameName).Deflate();
    }

    public static MetadataProperty GetPlatform(RawgPlatform platform)
    {
        return platform.Platform.Slug switch
        {
            "pc" => new MetadataSpecProperty("pc_windows"), //assumption that doesn't work for dos games, but for those there's often no data to extrapolate a proper specid
            "linux" => new MetadataSpecProperty("pc_linux"),
            "xbox-old" => new MetadataSpecProperty("xbox"),
            "xbox360" => new MetadataSpecProperty("xbox360"),
            "xbox-one" => new MetadataSpecProperty("xbox_one"),
            "xbox-series-x" => new MetadataSpecProperty("xbox_series"),
            "playstation1" => new MetadataSpecProperty("sony_playstation"),
            "playstation2" or "playstation3" or "playstation4" or "playstation5" or "psp"
                => new MetadataSpecProperty("sony_" + platform.Platform.Slug),
            "ps-vita" => new MetadataSpecProperty("sony_vita"),
            "nes" => new MetadataSpecProperty("nintendo_nes"),
            "snes" => new MetadataSpecProperty("nintendo_super_nes"),
            "nintendo-ds" => new MetadataSpecProperty("nintendo_ds"),
            "nintendo-3ds" => new MetadataSpecProperty("nintendo_3ds"),
            "nintendo-switch" => new MetadataSpecProperty("nintendo_switch"),
            "nintendo-64" => new MetadataSpecProperty("nintendo_64"),
            "gamecube" => new MetadataSpecProperty("nintendo_gamecube"),
            "wii" => new MetadataSpecProperty("nintendo_wii"),
            "wii-u" => new MetadataSpecProperty("nintendo_wiiu"),
            "game-boy" or "game-boy-color" or "game-boy-advance"
                => new MetadataSpecProperty("nintendo_" + platform.Platform.Slug.Replace("-", "")),
            "macintosh" => new MetadataSpecProperty(platform.Platform.Slug),
            "apple-ii" => new MetadataSpecProperty("apple_2"),
            "jaguar" => new MetadataSpecProperty("atari_jaguar"),
            "commodore-amiga" or "atari-2600" or "atari-5200" or "atari-7800" or "atari-8-bit" or "atari-st" or "atari-lynx" or "sega-saturn" or "sega-cd" or "sega-32x"
                => new MetadataSpecProperty(platform.Platform.Slug.Replace("-", "_")),
            "genesis" => new MetadataSpecProperty("sega_genesis"),
            "sega-master-system" => new MetadataSpecProperty("sega_mastersystem"),
            "dreamcast" => new MetadataSpecProperty("sega_dreamcast"),
            "game-gear" => new MetadataSpecProperty("sega_gamegear"),
            "3do" => new MetadataSpecProperty("3do"),
            _ => new MetadataNameProperty(platform.Platform.Name)
        };
    }

    public static ReleaseDate? ParseReleaseDate(RawgGameBase data, ILogger logger)
    {
        return data.Released.ParseReleaseDate(logger);
    }

    public static int? ParseUserScore(float? userScore)
    {
        if (userScore is null or 0)
            return null;

        return Convert.ToInt32(userScore.Value * 20);
    }

    public static Link GetRawgLink(RawgGameBase data)
    {
        return new Link("RAWG", $"https://rawg.io/games/{data.Id}");
    }

    public static List<Link> GetLinks(RawgGameDetails data)
    {
        List<Link> links = [GetRawgLink(data)];

        if (!string.IsNullOrWhiteSpace(data.Website))
            links.Add(new Link("Website", data.Website));

        if (!string.IsNullOrWhiteSpace(data.RedditUrl))
            links.Add(new Link("Reddit", data.RedditUrl));

        return links;
    }

    public static RawgGameBase GetExactTitleMatch(Game game, RawgApiClient client, IPlayniteAPI playniteApi, bool setLink = true)
    {
        if (string.IsNullOrWhiteSpace(game?.Name))
            return null;

        string searchString;
        if (game.ReleaseYear.HasValue)
            searchString = $"{game.Name} {game.ReleaseYear}";
        else
            searchString = game.Name;
        var result = client.SearchGames(searchString);
        if (result?.Results == null)
            return null;

        var foundGame =
            MatchGame(game, result.Results, playniteApi, matchPlatform: true, matchReleaseYear: true, matchNameExact: true, setLink)
            ?? MatchGame(game, result.Results, playniteApi, matchPlatform: true, matchReleaseYear: false, matchNameExact: true, setLink)
            ?? MatchGame(game, result.Results, playniteApi, matchPlatform: true, matchReleaseYear: true, matchNameExact: false, setLink)
            ?? MatchGame(game, result.Results, playniteApi, matchPlatform: false, matchReleaseYear: false, matchNameExact: true, setLink)
            ?? MatchGame(game, result.Results, playniteApi, matchPlatform: false, matchReleaseYear: true, matchNameExact: false, setLink)
            ?? MatchGame(game, result.Results, playniteApi, setLink: setLink);
        return foundGame;
    }

    private static RawgGameBase MatchGame(Game game, IEnumerable<RawgGameBase> searchResults, IPlayniteAPI playniteApi, bool matchPlatform = false, bool matchReleaseYear = false, bool matchNameExact = false, bool setLink = true)
    {
        string normalizedGameName = matchNameExact ? StripYear(game.Name) : NormalizeNameForComparison(game.Name);
        foreach (var searchResultGame in searchResults)
        {
            string searchResultGameName = matchNameExact ? StripYear(searchResultGame.Name) : NormalizeNameForComparison(searchResultGame.Name);

            if (!normalizedGameName.Equals(searchResultGameName, StringComparison.InvariantCultureIgnoreCase))
                continue;


            if (matchPlatform && searchResultGame.Platforms?.Any() == true && game.PlatformIds?.Any() == true)
            {
                var gamePlatforms = game.Platforms;
                bool matched = false;
                foreach (var rawgPlatform in searchResultGame.Platforms)
                {
                    var p = GetPlatform(rawgPlatform);
                    foreach (var playnitePlatform in gamePlatforms)
                    {
                        if ((p is MetadataSpecProperty platformSpec && platformSpec.Id == playnitePlatform.SpecificationId)
                            || (p is MetadataNameProperty platformName && platformName.Name == playnitePlatform.Name))
                        {
                            matched = true;
                            break;
                        }
                    }
                }
                if (!matched)
                    continue;
            }

            var releaseDate = ParseReleaseDate(searchResultGame, null);
            if (matchReleaseYear && game.ReleaseYear.HasValue && releaseDate.HasValue && releaseDate?.Year != game.ReleaseYear)
                continue;

            if (setLink)
                SetLink(game, searchResultGame, playniteApi);
            return searchResultGame;
        }
        return null;
    }

    public static Guid RawgLibraryId = Guid.Parse("e894b739-2d6e-41ee-aed4-2ea898e29803");

    /// <summary>
    /// Set a link to more easily find the RAWG game in the future. This saves having to search for the game every time, which would burn through API key use limits.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="rawgGame"></param>
    /// <param name="playniteApi"></param>
    private static void SetLink(Game game, RawgGameBase rawgGame, IPlayniteAPI playniteApi)
    {
        //The link doesn't need to be set if the RAWG ID is already the GameId
        if (game.PluginId == RawgLibraryId)
            return;

        //TODO: remove once metadata collection merging is in
        //This is here to prevent new games from getting this link and metadata collection then not happening for the game's links
        if (game.Links == null || game.Links.Count == 0)
            return;

        var rawgLink = GetRawgLink(rawgGame);
        if (game.Links?.Any(l => l.Url == rawgLink.Url) == true)
            return;

        System.Collections.ObjectModel.ObservableCollection<Link> links;
        if (game.Links == null)
            links = [];
        else
            links = new(game.Links);

        links.Add(rawgLink);
        game.Links = links;
        playniteApi.Database.Games.Update(game);
    }

    private static readonly RawgIdUtility IdUtility = new();

    public static int? GetRawgIdFromGame(Game game)
    {
        var stringId = IdUtility.GetIdsFromGame(game).FirstOrDefault(i => i.Database == ExternalDatabase.RAWG).Id;
        if (int.TryParse(stringId, out int id))
            return id;

        return null;
    }

    public static GameMetadata ToGameMetadata(RawgGameDetails data, ILogger logger, RawgBaseSettings settings)
    {
        return new GameMetadata
        {
            GameId = data.Id.ToString(),
            Name = StripYear(data.Name),
            Description = data.Description,
            ReleaseDate = ParseReleaseDate(data, logger),
            CriticScore = data.Metacritic,
            CommunityScore = ParseUserScore(data.Rating),
            Platforms = data.Platforms.NullIfEmpty()?.Select(GetPlatform).ToHashSet(),
            BackgroundImage = data.BackgroundImage != null ? new MetadataFile(data.BackgroundImage) : null,
            Tags = data.Tags.NullIfEmpty()?.Where(t => t.Language == settings.LanguageCode).Select(t => new MetadataNameProperty(t.Name)).ToHashSet<MetadataProperty>(),
            Genres = data.Genres.NullIfEmpty()?.Select(g => new MetadataNameProperty(g.Name)).ToHashSet<MetadataProperty>(),
            Developers = data.Developers.NullIfEmpty()?.Select(d => new MetadataNameProperty(d.Name.TrimCompanyForms())).ToHashSet<MetadataProperty>(),
            Publishers = data.Publishers.NullIfEmpty()?.Select(p => new MetadataNameProperty(p.Name.TrimCompanyForms())).ToHashSet<MetadataProperty>(),
            Links = GetLinks(data).NullIfEmpty()?.ToList(),
        };
    }
}
