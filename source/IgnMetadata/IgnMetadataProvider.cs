using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnMetadata
{
    public class IgnMetadataProvider : OnDemandMetadataProvider
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly MetadataRequestOptions options;
        private readonly IgnMetadata plugin;
        private readonly IgnClient client;
        private readonly IPlatformUtility platformUtility;
        private IgnGame ignGameData;
        private bool dataIsDetails = false;

        public override List<MetadataField> AvailableFields { get; } = new List<MetadataField>
        {
            MetadataField.CoverImage,
            MetadataField.Name,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Genres,
            MetadataField.Features,
            MetadataField.Series,
            MetadataField.Description,
            MetadataField.AgeRating,
            MetadataField.ReleaseDate,
            MetadataField.Platform,
        };

        public IgnMetadataProvider(MetadataRequestOptions options, IgnMetadata plugin, IgnClient client, IPlatformUtility platformUtility)
        {
            this.options = options;
            this.plugin = plugin;
            this.client = client;
            this.platformUtility = platformUtility;
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            var data = GetSearchResultData();
            if (IsEmpty(data) || data.PrimaryImage?.Url == null)
                return null;

            return new MetadataFile(data.PrimaryImage?.Url);
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            var data = GetSearchResultData();
            if (IsEmpty(data))
                return null;

            return data.Metadata?.Names?.Name;
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            var data = GetSearchResultData();
            if (IsEmpty(data))
                return null;

            return data.Producers.NullIfEmpty()?.Select(x => new MetadataNameProperty(x.Name.TrimCompanyForms()));
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            var data = GetSearchResultData();
            if (IsEmpty(data))
                return null;

            return data.Publishers.NullIfEmpty()?.Select(x => new MetadataNameProperty(x.Name.TrimCompanyForms()));
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            var data = GetSearchResultData();
            if (IsEmpty(data))
                return null;

            return data.Genres.NullIfEmpty()?.Select(x => new MetadataNameProperty(x.Name));
        }

        public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
        {
            var data = GetSearchResultData();
            if (IsEmpty(data))
                return null;

            return data.Features.NullIfEmpty()?.Select(x => new MetadataNameProperty(x.Name));
        }

        public override IEnumerable<MetadataProperty> GetSeries(GetMetadataFieldArgs args)
        {
            var data = GetSearchResultData();
            if (IsEmpty(data))
                return null;

            return data.Franchises.NullIfEmpty()?.Select(x => new MetadataNameProperty(x.Name));
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            var data = GetDetails();
            if (IsEmpty(data))
                return null;

            return data.Metadata?.Descriptions?.Long ?? data.Metadata?.Descriptions?.Short;
        }

        public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
        {
            var data = GetDetails();
            if (IsEmpty(data) || data?.ObjectRegions == null)
                yield break;

            foreach (var region in data.ObjectRegions)
            {
                if (region.AgeRating == null)
                    continue;

                string rating = $"{region.AgeRating.AgeRatingType} {region.AgeRating.Name}";
                yield return new MetadataNameProperty(rating);
            }
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            var data = GetSearchResultData();
            if (IsEmpty(data))
                return null;

            var dateString = GetEarliestReleaseDate(data);
            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime date))
            {
                return new ReleaseDate(date);
            }
            else
            {
                logger.Warn($"Could not parse date \"{dateString}\"");
                return null;
            }
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            var data = GetSearchResultData();
            if (IsEmpty(data) || data?.ObjectRegions == null)
                return null;

            var platforms = data.ObjectRegions.SelectMany(r => r.Releases).SelectMany(r => r.PlatformAttributes).Select(x => x.Name).ToHashSet();

            return platforms.SelectMany(platformUtility.GetPlatforms).ToHashSet().NullIfEmpty();
        }

        private IgnGame GetSearchResultData()
        {
            if (ignGameData != null)
                return ignGameData;

            if (options.IsBackgroundDownload)
            {
                if (string.IsNullOrWhiteSpace(options.GameData.Name))
                {
                    return ignGameData = new IgnGame();
                }

                var searchResult = client.Search(options.GameData.Name);

                if (searchResult == null)
                {
                    return ignGameData = new IgnGame();
                }

                var nameToMatch = options.GameData.Name.Deflate();

                foreach (var g in searchResult)
                {
                    var namesObj = g.Metadata?.Names;
                    if (namesObj == null)
                        continue;

                    var gameNames = new List<string> { namesObj.Name.Deflate(), namesObj.Short.Deflate() };
                    if (namesObj.Alt != null)
                        gameNames.AddRange(namesObj.Alt.Select(a => a.Deflate()));

                    foreach (var gameName in gameNames)
                    {
                        if (string.IsNullOrWhiteSpace(gameName))
                            continue;

                        if (nameToMatch.Equals(gameName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return ignGameData = g;
                        }
                    }
                }

                return ignGameData = new IgnGame();
            }
            else
            {
                var selectedGame = plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(null, (a) =>
                {
                    try
                    {
                        var searchResult = client.Search(a);
                        return searchResult.Select(r => new GenericSearchResultGame(r)).ToList<GenericItemOption>();
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, $"Failed to get RAWG search data for <{a}>");
                        return new List<GenericItemOption>();
                    }
                }, options.GameData.Name, string.Empty);

                return ignGameData = ((GenericSearchResultGame)selectedGame)?.Game ?? new IgnGame();
            }
        }

        private IgnGame GetDetails()
        {
            if (dataIsDetails && ignGameData != null)
                return ignGameData;

            var searchResult = GetSearchResultData();
            if (IsEmpty(searchResult))
            {
                dataIsDetails = true;
                return searchResult;
            }
            var region = searchResult.ObjectRegions.Select(r => r.Region).SkipWhile(string.IsNullOrWhiteSpace).FirstOrDefault()?.ToLowerInvariant();

            dataIsDetails = true;
            return ignGameData = client.Get(searchResult.Slug, region) ?? new IgnGame();
        }


        private class GenericSearchResultGame : GenericItemOption
        {
            public GenericSearchResultGame(IgnGame g) : base(g.Metadata?.Names?.Name, string.Empty)
            {
                Game = g;
                var releaseDates = g.ObjectRegions.SelectMany(r => r.Releases).Where(r => !string.IsNullOrWhiteSpace(r.Date)).Select(r => r.Date).OrderBy(d => d).ToList();
                Description = GetEarliestReleaseDate(g);
            }

            public IgnGame Game { get; set; }
        }

        public static string GetEarliestReleaseDate(IgnGame g)
        {
            var releaseDates = g.ObjectRegions.SelectMany(r => r.Releases).Where(r => !string.IsNullOrWhiteSpace(r.Date)).Select(r => r.Date).OrderBy(d => d).ToList();
            return releaseDates.FirstOrDefault();
        }

        private bool IsEmpty(IgnGame ignGame)
        {
            return ignGame?.Slug == null;
        }
    }
}