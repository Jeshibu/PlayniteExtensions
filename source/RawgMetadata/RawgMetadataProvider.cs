using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using Rawg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RawgMetadata
{
    public class RawgMetadataProvider : OnDemandMetadataProvider
    {
        private RawgGameDetails foundGameData;
        private readonly MetadataRequestOptions options;
        private readonly RawgMetadata plugin;
        private readonly RawgApiClient client;
        private readonly string languageCode;
        private readonly ILogger logger = LogManager.GetLogger();
        private RawgGameDetails FoundGameData
        {
            get { return foundGameData; }
            set
            {
                foundGameData = value;
                FoundSearchResult = value;
            }
        }
        private RawgGameBase FoundSearchResult { get; set; }

        public override List<MetadataField> AvailableFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Description,
            MetadataField.ReleaseDate,
            MetadataField.CriticScore,
            MetadataField.CommunityScore,
            MetadataField.Platform,
            MetadataField.BackgroundImage,
            MetadataField.Tags,
            MetadataField.Genres,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Links
        };

        public RawgMetadataProvider(MetadataRequestOptions options, RawgMetadata plugin, RawgApiClient client, string languageCode = "eng")
        {
            this.options = options;
            this.plugin = plugin;
            this.client = client;
            this.languageCode = languageCode;
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            var data = GetSearchResult();
            if (IsEmpty(data))
                return base.GetName(args);

            return RawgMetadataHelper.StripYear(FoundSearchResult.Name);
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            var data = GetFullGameDetails();
            if (IsEmpty(data))
                return base.GetDescription(args);

            return data.Description;
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            var data = GetSearchResult();
            if (IsEmpty(data) || string.IsNullOrWhiteSpace(data.Released))
                return base.GetReleaseDate(args);

            return RawgMetadataHelper.ParseReleaseDate(data, logger);
        }

        public override int? GetCriticScore(GetMetadataFieldArgs args)
        {
            var data = GetSearchResult();
            if (IsEmpty(data))
                return base.GetCriticScore(args);

            return data.Metacritic;
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            var data = GetSearchResult();
            if (IsEmpty(data) || data.Rating == null || data.Rating == 0)
                return base.GetCommunityScore(args);

            return Convert.ToInt32(data.Rating.Value * 20);
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            var data = GetSearchResult();
            if (IsEmpty(data))
                return base.GetPlatforms(args);

            var platforms = data.Platforms.NullIfEmpty()?.Select(RawgMetadataHelper.GetPlatform);
            return platforms;
        }

        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            if (options.IsBackgroundDownload)
            {
                var data = GetSearchResult();
                if (IsEmpty(data))
                    return base.GetBackgroundImage(args);

                return new MetadataFile(data.BackgroundImage);
            }
            else
            {
                var data = GetFullGameDetails();
                if (IsEmpty(data))
                    return base.GetBackgroundImage(args);

                var imageOptions = new List<ImageFileOption>();
                if (!string.IsNullOrWhiteSpace(data.BackgroundImage))
                    imageOptions.Add(new ImageFileOption(data.BackgroundImage));

                if (!string.IsNullOrWhiteSpace(data.BackgroundImageAdditional))
                    imageOptions.Add(new ImageFileOption(data.BackgroundImageAdditional));

                var screenshots = client.GetScreenshots(data.Slug);
                imageOptions.AddRange(screenshots.Select(s => new ImageFileOption(s.Image)));

                var chosen = plugin.PlayniteApi.Dialogs.ChooseImageFile(imageOptions, "Choose a background");
                if (chosen == null)
                    return null;
                else
                    return new MetadataFile(chosen.Path);
            }
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            var data = GetSearchResult();
            if (IsEmpty(data))
                return base.GetTags(args);

            var tags = data.Tags.Where(t => t.Language == languageCode).Select(t => new MetadataNameProperty(t.Name)).ToList();
            return tags.NullIfEmpty();
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            var data = GetSearchResult();
            if (IsEmpty(data))
                return base.GetGenres(args);

            var genres = data.Genres.NullIfEmpty()?.Select(g => new MetadataNameProperty(g.Name));
            return genres;
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            var data = GetFullGameDetails();
            if (IsEmpty(data))
                return base.GetDevelopers(args);

            var developers = data.Developers.NullIfEmpty()?.Select(d => new MetadataNameProperty(d.Name));
            return developers;
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            var data = GetFullGameDetails();
            if (IsEmpty(data))
                return base.GetPublishers(args);

            var publishers = data.Publishers.NullIfEmpty()?.Select(p => new MetadataNameProperty(p.Name));
            return publishers;
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            var data = GetFullGameDetails();
            if (IsEmpty(data))
                return base.GetLinks(args);

            var links = new List<Link>();
            links.Add(new Link("RAWG", $"https://rawg.io/games/{data.Id}"));

            if (!string.IsNullOrWhiteSpace(data.Website))
                links.Add(new Link("Website", data.Website));

            if (!string.IsNullOrWhiteSpace(data.RedditUrl))
                links.Add(new Link("Reddit", data.RedditUrl));

            return links.NullIfEmpty();
        }

        private RawgGameBase GetSearchResult()
        {
            if (FoundSearchResult != null)
                return FoundSearchResult;


            if (options.IsBackgroundDownload)
            {
                var searchResult = RawgMetadataHelper.GetExactTitleMatch(options.GameData, client, plugin.PlayniteApi);
                return FoundSearchResult = searchResult ?? new RawgGameBase();
            }
            else
            {
                var selectedGame = plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(null, (a) =>
                {
                    try
                    {
                        var searchResult = client.SearchGames(a);
                        return searchResult.Results.Select(r => new GenericSearchResultGame(r)).ToList<GenericItemOption>();
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, $"Failed to get RAWG search data for <{a}>");
                        return new List<GenericItemOption>();
                    }
                }, options.GameData.Name, string.Empty);


                return FoundSearchResult = ((GenericSearchResultGame)selectedGame)?.Game ?? new RawgGameBase();
            }
        }

        private RawgGameDetails GetFullGameDetails()
        {
            if (FoundGameData != null)
                return FoundGameData;

            var searchResult = GetSearchResult();
            if (IsEmpty(searchResult))
                return FoundGameData = new RawgGameDetails();

            FoundGameData = client.GetGame(searchResult.Slug) ?? new RawgGameDetails();
            return FoundGameData;
        }

        private class GenericSearchResultGame : GenericItemOption
        {
            public GenericSearchResultGame(RawgGameBase g) : base(g.Name, g.Released)
            {
                Game = g;
            }

            public RawgGameBase Game { get; set; }
        }

        private bool IsEmpty(RawgGameBase rawgGame)
        {
            return rawgGame?.Slug == null;
        }
    }
}