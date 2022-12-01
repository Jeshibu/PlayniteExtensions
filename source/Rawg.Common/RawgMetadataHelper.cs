using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rawg.Common
{
    public static class RawgMetadataHelper
    {
        private static Regex yearRegex = new Regex(@" \([0-9]{4}\)$", RegexOptions.Compiled);

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
            switch (platform.Platform.Slug)
            {
                case "pc":
                    return new MetadataSpecProperty("pc_windows"); //assumption that doesn't work for dos games, but for those there's often no data to extrapolate a proper specid
                case "linux":
                    return new MetadataSpecProperty("pc_linux");

                case "xbox-old":
                    return new MetadataSpecProperty("xbox");
                case "xbox360":
                    return new MetadataSpecProperty("xbox360");
                case "xbox-one":
                    return new MetadataSpecProperty("xbox_one");
                case "xbox-series-x":
                    return new MetadataSpecProperty("xbox_series");

                case "playstation1":
                    return new MetadataSpecProperty("sony_playstation");
                case "playstation2":
                case "playstation3":
                case "playstation4":
                case "playstation5":
                case "psp":
                    return new MetadataSpecProperty("sony_" + platform.Platform.Slug);
                case "ps-vita":
                    return new MetadataSpecProperty("sony_vita");

                case "nes":
                    return new MetadataSpecProperty("nintendo_nes");
                case "snes":
                    return new MetadataSpecProperty("nintendo_super_nes");
                case "nintendo-ds":
                    return new MetadataSpecProperty("nintendo_ds");
                case "nintendo-3ds":
                    return new MetadataSpecProperty("nintendo_3ds");
                case "nintendo-switch":
                    return new MetadataSpecProperty("nintendo_switch");
                case "nintendo-64":
                    return new MetadataSpecProperty("nintendo_64");
                case "gamecube":
                    return new MetadataSpecProperty("nintendo_gamecube");
                case "wii":
                    return new MetadataSpecProperty("nintendo_wii");
                case "wii-u":
                    return new MetadataSpecProperty("nintendo_wiiu");
                case "game-boy":
                case "game-boy-color":
                case "game-boy-advance":
                    return new MetadataSpecProperty("nintendo_" + platform.Platform.Slug.Replace("-", ""));
                case "macintosh":
                    return new MetadataSpecProperty(platform.Platform.Slug);
                case "apple-ii":
                    return new MetadataSpecProperty("apple_2");

                case "jaguar":
                    return new MetadataSpecProperty("atari_jaguar");
                case "commodore-amiga":
                case "atari-2600":
                case "atari-5200":
                case "atari-7800":
                case "atari-8-bit":
                case "atari-st":
                case "atari-lynx":
                case "sega-saturn":
                case "sega-cd":
                case "sega-32x":
                    return new MetadataSpecProperty(platform.Platform.Slug.Replace("-", "_"));

                case "genesis":
                    return new MetadataSpecProperty("sega_genesis");
                case "sega-master-system":
                    return new MetadataSpecProperty("sega_mastersystem");
                case "dreamcast":
                    return new MetadataSpecProperty("sega_dreamcast");
                case "game-gear":
                    return new MetadataSpecProperty("sega_gamegear");
                case "3do":
                    return new MetadataSpecProperty("3do");

                case "atari-xegs":
                case "atari-flashback":
                case "ios":
                case "android":
                case "macos":
                case "neogeo":
                case "nintendo-dsi":
                default:
                    return new MetadataNameProperty(platform.Platform.Name);
            }
        }

        public static ReleaseDate? ParseReleaseDate(RawgGameBase data, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(data.Released))
                return null;

            var segments = data.Released.Split('-');
            List<int> numberSegments;
            try
            {
                numberSegments = segments.Select(int.Parse).ToList();
            }
            catch (Exception ex)
            {
                logger?.Warn(ex, $"Could not parse release date <{data.Released}> for {data.Name}");
                return null;
            }

            switch (numberSegments.Count)
            {
                case 1:
                    return new ReleaseDate(numberSegments[0]);
                case 2:
                    return new ReleaseDate(numberSegments[0], numberSegments[1]);
                case 3:
                    return new ReleaseDate(numberSegments[0], numberSegments[1], numberSegments[2]);
                default:
                    logger?.Warn($"Could not parse release date <{data.Released}> for {data.Name}");
                    return null;
            }
        }

        public static int? ParseUserScore(float? userScore)
        {
            if (userScore == null || userScore == 0)
                return null;

            return Convert.ToInt32(userScore.Value * 20);
        }

        public static Link GetRawgLink(RawgGameBase data)
        {
            return new Link("RAWG", $"https://rawg.io/games/{data.Id}");
        }

        public static List<Link> GetLinks(RawgGameDetails data)
        {
            var links = new List<Link>();
            links.Add(GetRawgLink(data));

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
                ?? MatchGame(game, result.Results, playniteApi, matchPlatform: true, matchReleaseYear: true, matchNameExact: false, setLink)
                ?? MatchGame(game, result.Results, playniteApi, matchPlatform: true, matchReleaseYear: false, matchNameExact: true, setLink)
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

        private static Guid RawgLibraryId = Guid.Parse("e894b739-2d6e-41ee-aed4-2ea898e29803");

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
                links = new System.Collections.ObjectModel.ObservableCollection<Link>();
            else
                links = new System.Collections.ObjectModel.ObservableCollection<Link>(game.Links);

            links.Add(rawgLink);
            game.Links = links;
            playniteApi.Database.Games.Update(game);
        }
    }
}
