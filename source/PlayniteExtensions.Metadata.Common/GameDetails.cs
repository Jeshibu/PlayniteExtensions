using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteExtensions.Metadata.Common;

public class GameDetails
{
    public string Id { get; set; }

    public List<string> Names { get; set; } = [];

    public string Description { get; set; }

    public ReleaseDate? ReleaseDate { get; set; }

    public List<Link> Links { get; set; } = [];

    public int? CriticScore { get; set; }

    public int? CommunityScore { get; set; }

    public List<IImageData> IconOptions { get; set; } = [];

    public List<IImageData> CoverOptions { get; set; } = [];

    public List<IImageData> BackgroundOptions { get; set; } = [];

    public List<string> Series { get; set; } = [];

    public List<string> AgeRatings { get; set; } = [];

    public List<MetadataProperty> Platforms { get; set; } = [];

    public List<string> Developers { get; set; } = [];

    public List<string> Publishers { get; set; } = [];

    public List<string> Genres { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public List<string> Features { get; set; } = [];

    public List<DbId> ExternalIds { get; set; } = [];

    public ulong? InstallSize { get; set; }

    public string Url { get; set; }

    public string Version { get; set; }

    public GameMetadata ToMetadata(IPlayniteAPI playniteAPI = null)
    {
        var metadata = new GameMetadata()
        {
            Name = Names.FirstOrDefault(),
            Description = Description,
            ReleaseDate = ReleaseDate,
            Links = Links.NullIfEmpty()?.ToList(),
            CriticScore = CriticScore,
            CommunityScore = CommunityScore,
            Icon = SelectImage(IconOptions, playniteAPI, "LOCSelectIconTitle"),
            CoverImage = SelectImage(CoverOptions, playniteAPI, "LOCSelectCoverTitle"),
            BackgroundImage = SelectImage(BackgroundOptions, playniteAPI, "LOCSelectBackgroundTitle"),
            Series = ToMetadataProperties(Series),
            AgeRatings = ToMetadataProperties(AgeRatings),
            Platforms = Platforms.NullIfEmpty()?.ToHashSet(),
            Developers = ToMetadataProperties(Developers),
            Publishers = ToMetadataProperties(Publishers),
            Genres = ToMetadataProperties(Genres),
            Tags = ToMetadataProperties(Tags),
            Features = ToMetadataProperties(Features),
            InstallSize = InstallSize,
            Version = Version,
        };
        return metadata;
    }

    private MetadataFile SelectImage(List<IImageData> images, IPlayniteAPI playniteAPI = null, string titleResourceKey = null)
    {
        if (images == null || images.Count == 0) return null;

        if (playniteAPI == null)
            return new MetadataFile(images.First().Url);

        var chosen = playniteAPI.Dialogs.ChooseImageFile(
            images.Select(i => new ImageFileOption(i.Url)).ToList(),
            playniteAPI?.Resources.GetString(titleResourceKey));

        return chosen == null ? null : new MetadataFile(chosen.Path);
    }

    private HashSet<MetadataProperty> ToMetadataProperties(List<string> names)
    {
        if (names == null || names.Count == 0) return null;
        return names.Select(n => new MetadataNameProperty(n)).ToHashSet<MetadataProperty>();
    }

    public override string ToString()
    {
        if (Names == null)
            return base.ToString();

        var names = string.Join(" / ", Names);
        return $"{names} ({Id})";
    }
}