using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteExtensions.Metadata.Common
{
    public class GameDetails
    {
        public List<string> Names { get; set; } = new List<string>();

        public string Description { get; set; }

        public ReleaseDate? ReleaseDate { get; set; }

        public List<Link> Links { get; set; } = new List<Link>();

        public int? CriticScore { get; set; }

        public int? CommunityScore { get; set; }

        public List<IImageData> IconOptions { get; set; } = new List<IImageData>();

        public List<IImageData> CoverOptions { get; set; } = new List<IImageData>();

        public List<IImageData> BackgroundOptions { get; set; } = new List<IImageData>();

        public List<string> Series { get; set; } = new List<string>();

        public List<string> AgeRatings { get; set; } = new List<string>();

        public List<MetadataProperty> Platforms { get; set; } = new List<MetadataProperty>();

        public List<string> Developers { get; set; } = new List<string>();

        public List<string> Publishers { get; set; } = new List<string>();

        public List<string> Genres { get; set; } = new List<string>();

        public List<string> Tags { get; set; } = new List<string>();

        public List<string> Features { get; set; } = new List<string>();

        public ulong? InstallSize { get; set; }

        public string Url { get; set; }

        public GameMetadata ToMetadata()
        {
            var metadata = new GameMetadata()
            {
                Name = Names.FirstOrDefault(),
                Description = Description,
                ReleaseDate = ReleaseDate,
                Links = Links.NullIfEmpty()?.ToList(),
                CriticScore = CriticScore,
                CommunityScore = CommunityScore,
                Icon = GetFirstImage(IconOptions),
                CoverImage = GetFirstImage(CoverOptions),
                BackgroundImage = GetFirstImage(BackgroundOptions),
                Series = ToMetadataProperties(Series),
                AgeRatings = ToMetadataProperties(AgeRatings),
                Platforms = Platforms.NullIfEmpty()?.ToHashSet(),
                Developers = ToMetadataProperties(Developers),
                Publishers = ToMetadataProperties(Publishers),
                Genres = ToMetadataProperties(Genres),
                Tags = ToMetadataProperties(Tags),
                Features = ToMetadataProperties(Features),
                InstallSize = InstallSize,
            };
            return metadata;
        }

        private MetadataFile GetFirstImage(List<IImageData> images)
        {
            if (images == null || images.Count == 0) return null;
            return new MetadataFile(images.First().Url);
        }

        private HashSet<MetadataProperty> ToMetadataProperties(List<string> names)
        {
            if (names == null || names.Count == 0) return null;
            return names.Select(n=>new MetadataNameProperty(n)).ToHashSet<MetadataProperty>();
        }
    }
}