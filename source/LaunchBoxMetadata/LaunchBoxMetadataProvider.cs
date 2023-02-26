using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayniteExtensions.Common;
using Playnite.SDK.Models;
using System.Text.RegularExpressions;

namespace LaunchBoxMetadata
{
    public class LaunchBoxMetadataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions options;
        private readonly LaunchBoxMetadata plugin;
        private readonly LaunchBoxDatabase database;
        private readonly IPlatformUtility platformUtility;
        private LaunchBoxGame foundGame;

        public override List<MetadataField> AvailableFields => plugin.SupportedFields;

        public LaunchBoxMetadataProvider(MetadataRequestOptions options, LaunchBoxMetadata plugin, LaunchBoxDatabase database, IPlatformUtility platformUtility)
        {
            this.options = options;
            this.plugin = plugin;
            this.database = database;
            this.platformUtility = platformUtility;
        }

        private LaunchBoxGame FindGame()
        {
            if (foundGame != null)
                return foundGame;

            if (options.IsBackgroundDownload)
            {
                var results = database.SearchGames(options.GameData.Name, 100);
                var deflatedSearchGameName = options.GameData.Name.Deflate();
                var platformSpecs = options.GameData.Platforms?.Where(p => p.SpecificationId != null).Select(p => p.SpecificationId).ToList();
                var platformNames = options.GameData.Platforms?.Select(p => p.Name).ToList();
                foreach (var game in results)
                {
                    var deflatedMatchedGameName = game.MatchedName.Deflate();
                    if (!deflatedSearchGameName.Equals(deflatedMatchedGameName, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    if (options.GameData.Platforms?.Count > 0)
                    {
                        var platforms = platformUtility.GetPlatforms(game.Platform);
                        foreach (var platform in platforms)
                        {
                            if (platform is MetadataSpecProperty specPlatform && platformSpecs.Contains(specPlatform.Id))
                            {
                                return foundGame = game;
                            }
                            else if (platform is MetadataNameProperty namePlatform && platformNames.Contains(namePlatform.Name))
                            {
                                return foundGame = game;
                            }
                        }
                    }
                    else
                    {
                        //no platforms to match, so consider a name match a success
                        return foundGame = game;
                    }
                }
                return foundGame = new LaunchBoxGame();
            }
            else
            {
                var chosen = plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(null, s =>
                {
                    var results = database.SearchGames(s).Select(LaunchBoxGameItemOption.FromLaunchBoxGame).ToList<GenericItemOption>();
                    return results;
                }, options.GameData.Name, "LaunchBox: select game");
                if (chosen == null)
                    return foundGame = new LaunchBoxGame();
                else
                    return foundGame = ((LaunchBoxGameItemOption)chosen).Game;
            }
        }

        private IEnumerable<LaunchBoxGameImage> GetImages()
        {
            var id = FindGame().DatabaseID;
            if (id == null)
                return new LaunchBoxGameImage[0];

            return database.GetGameImages(id);
        }

        private class LaunchBoxGameItemOption : GenericItemOption
        {
            public LaunchboxGameSearchResult Game { get; set; }

            public static LaunchBoxGameItemOption FromLaunchBoxGame(LaunchboxGameSearchResult g)
            {
                var name = g.Name;
                if (g.MatchedName != g.Name)
                    name += $" ({g.MatchedName})";

                var descriptionItems = new List<string>();
                if (g.ReleaseDate.HasValue)
                    descriptionItems.Add(g.ReleaseDate.Value.ToString("yyyy-MM-dd"));
                if (g.ReleaseYear != 0)
                    descriptionItems.Add(g.ReleaseYear.ToString());
                descriptionItems.Add(g.Platform);

                string description = string.Join(" | ", descriptionItems);

                return new LaunchBoxGameItemOption
                {
                    Game = g,
                    Name = name,
                    Description = description,
                };
            }
        }

        private IEnumerable<MetadataProperty> Split(string str, Func<string,string> stringSelector = null)
        {
            if (string.IsNullOrWhiteSpace(str))
                return null;

            var split = str.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var strings = stringSelector == null ? split : split.Select(stringSelector);
            var output = strings.Select(g => new MetadataNameProperty(g.Trim()));

            return output;
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            return FindGame().Name ?? base.GetName(args);
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            var overview = FindGame().Overview;
            if (overview == null)
                return base.GetDescription(args);

            overview = Regex.Replace(overview, "\r?\n", "<br>$0");
            return overview;
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            string platform = FindGame().Platform;
            if (platform == null)
                return base.GetPlatforms(args);

            return platformUtility.GetPlatforms(platform);
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            var commScore = FindGame().CommunityRating;
            if (commScore == 0)
                return base.GetCommunityScore(args);

            return (int)(commScore * 20); //from 0-5 to 0-100 range
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            var game = FindGame();

            if (game.ReleaseDate.HasValue)
                return new ReleaseDate(game.ReleaseDate.Value);

            if (game.ReleaseYear != 0)
                return new ReleaseDate(game.ReleaseYear);

            return base.GetReleaseDate(args);
        }

        public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
        {
            var esrbRating = FindGame().ESRB;
            if (string.IsNullOrEmpty(esrbRating))
                return base.GetAgeRatings(args);

            return new[] { new MetadataNameProperty("ESRB " + esrbRating) };
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {

            return Split(FindGame().Genres) ?? base.GetGenres(args);
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            return Split(FindGame().Developer, StringExtensions.TrimCompanyForms) ?? base.GetDevelopers(args);
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            return Split(FindGame().Publisher, StringExtensions.TrimCompanyForms) ?? base.GetPublishers(args);
        }

        private MetadataFile PickImage(string caption, params string[] filters)
        {
            var images = GetImages().Where(i => filters.Any(f => i.Type.Contains(f)))
                                    .Select(i => new ImageFileOption("https://images.launchbox-app.com/" + i.FileName))
                                    .ToList();
            if (images.Count == 0)
                return null;

            ImageFileOption selected;
            if (options.IsBackgroundDownload || images.Count == 1)
            {
                selected = images.FirstOrDefault();
            }
            else
            {
                selected = plugin.PlayniteApi.Dialogs.ChooseImageFile(images, caption);
            }

            if (selected == null)
            {
                return null;
            }
            else
            {
                return new MetadataFile(selected.Path);
            }
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            return PickImage("Select cover", "Box - Front");
        }

        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            return PickImage("Select background", "Background", "Screenshot - Gameplay");
        }
    }
}