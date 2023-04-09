using MobyGamesMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobyGamesMetadata
{
    public class MobyGamesMetadataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions options;
        private readonly MobyGamesMetadata plugin;
        private readonly ISearchableDataSourceWithDetails<GameSearchResult, GameDetails> dataSource;
        private GameDetails foundGame;

        public override List<MetadataField> AvailableFields => plugin.SupportedFields;

        public MobyGamesMetadataProvider(MetadataRequestOptions options, MobyGamesMetadata plugin, ISearchableDataSourceWithDetails<GameSearchResult, GameDetails> dataSource)
        {
            this.options = options;
            this.plugin = plugin;
            this.dataSource = dataSource;
        }

        private GameDetails GetGame()
        {
            if (foundGame != null) return foundGame;

            if (options.IsBackgroundDownload)
            {
                if (string.IsNullOrWhiteSpace(options.GameData.Name)) return foundGame = new GameDetails();
                var searchResults = dataSource.Search(options.GameData.Name);
                var filteredSearchResults = new List<GameSearchResult>(searchResults);
                var platforms = options.GameData.Platforms;
                if (platforms != null && platforms.Any())
                    filteredSearchResults.RemoveAll(sr => !PlatformsOverlap(options.GameData, sr));

                var snc = new SortableNameConverter(new[] { "the", "a", "an" }, batchOperation: true, removeEditions: true);
                var comparisonName = snc.Convert(options.GameData.Name).Deflate();
                filteredSearchResults.RemoveAll(sr => !NamesOverlap(comparisonName, sr, snc));

                var perfectResult = filteredSearchResults.FirstOrDefault(sr => options.GameData.Name == sr.Title);
                var searchResult = perfectResult ?? filteredSearchResults.FirstOrDefault();
                if (searchResult == null)
                    return foundGame = new GameDetails();
                else
                    return foundGame = dataSource.GetDetails(searchResult);
            }
            else
            {
                var result = (GameSearchResult)plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(null, a => dataSource.Search(a).ToList<GenericItemOption>(), options.GameData.Name);
                if (result == null)
                    return foundGame = new GameDetails();

                return foundGame = dataSource.GetDetails(result);
            }
        }

        private bool PlatformsOverlap(Game searchGame, GameSearchResult sr)
        {
            foreach (var fp in sr.Platforms)
            {
                if (fp is MetadataSpecProperty specProperty)
                {
                    if (searchGame.Platforms.Any(p => specProperty.Id == p.SpecificationId))
                        return true;
                }
                else if (fp is MetadataNameProperty nameProperty)
                {
                    if (searchGame.Platforms.Any(p => nameProperty.Name == p.Name))
                        return true;
                }
            }
            return false;
        }

        private bool NamesOverlap(string normalizedSearchName, GameSearchResult sr, SortableNameConverter snc)
        {
            var allTitles = new List<string> { sr.Title };
            if (sr.AlternateTitles != null)
                allTitles.AddRange(sr.AlternateTitles);

            foreach (var title in allTitles)
            {
                var searchResultTitleNormalized = snc.Convert(title).Deflate();
                if (searchResultTitleNormalized.Equals(normalizedSearchName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            return GetGame().Names?.FirstOrDefault();
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            return GetGame().Description;
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            return GetGame().ReleaseDate;
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            return GetGame().Genres.NullIfEmpty()?.Select(g => new MetadataNameProperty(g));
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            return GetGame().Tags.NullIfEmpty()?.Select(g => new MetadataNameProperty(g));
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            return GetGame().Platforms.NullIfEmpty();
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            return GetGame().Developers.NullIfEmpty()?.Select(g => new MetadataNameProperty(g.TrimCompanyForms()));
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            return GetGame().Publishers.NullIfEmpty()?.Select(g => new MetadataNameProperty(g.TrimCompanyForms()));
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            var img = GetGame().CoverOptions.FirstOrDefault();
            if (img == null) return null;
            return new MetadataFile(img.Url);
        }

        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            var images = GetGame().BackgroundOptions;
            IImageData selectedImg;
            if (options.IsBackgroundDownload || images.Count < 2)
            {
                selectedImg = images.FirstOrDefault();
            }
            else
            {
                var options = images.Select(i => new ImgOption(i)).ToList<ImageFileOption>();
                var s = plugin.PlayniteApi.Dialogs.ChooseImageFile(options, "Select background image");
                selectedImg = ((ImgOption)s)?.Image;
            }
            if (selectedImg != null)
                return new MetadataFile(selectedImg.Url);
            else
                return null;
        }

        public override int? GetCriticScore(GetMetadataFieldArgs args)
        {
            return GetGame().CriticScore;
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            return GetGame().CommunityScore;
        }

        public override IEnumerable<MetadataProperty> GetSeries(GetMetadataFieldArgs args)
        {
            return GetGame().Series.NullIfEmpty()?.Select(s => new MetadataNameProperty(s));
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            return GetGame().Links;
        }

        private class ImgOption : ImageFileOption
        {
            public ImgOption(IImageData image)
            {
                Image = image;
                Path = image.ThumbnailUrl ?? image.Url;
            }

            public IImageData Image { get; }
        }
    }
}