using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Linq;

namespace Rawg.Common;

public static partial class RawgMetadataHelper
{
    public static GameDetails ToGameDetails(RawgGameDetails data, ILogger logger, RawgBaseSettings settings)
    {
        var gameDetails = new GameDetails
        {
            Id = data.Id.ToString(),
            Url = GetRawgLink(data).Url,
            Names = [data.Name],
            Description = data.Description,
            ReleaseDate = ParseReleaseDate(data, logger),
            CriticScore = data.Metacritic,
            CommunityScore = ParseUserScore(data.Rating),
            Platforms = data.Platforms.NullIfEmpty()?.Select(GetPlatform).ToList(),
            BackgroundOptions = data.BackgroundImage != null ? [new BasicImage(data.BackgroundImage)] : [],
            Tags = data.Tags.NullIfEmpty()?.Where(t => t.Language == settings.LanguageCode).Select(t => t.Name).ToList(),
            Genres = data.Genres.NullIfEmpty()?.Select(g => g.Name).ToList(),
            Developers = data.Developers.NullIfEmpty()?.Select(d => d.Name.TrimCompanyForms()).ToList(),
            Publishers = data.Publishers.NullIfEmpty()?.Select(p => p.Name.TrimCompanyForms()).ToList(),
            Links = GetLinks(data).NullIfEmpty()?.ToList(),
        };

        var nameWithoutYear = StripYear(data.Name);
        if (data.Name != nameWithoutYear)
            gameDetails.Names.Add(nameWithoutYear);

        return gameDetails;
    }

    public static List<IImageData> GetBackgroundOptions(RawgGameDetails data)
    {
        var imageOptions = new List<IImageData>();
        if (!string.IsNullOrWhiteSpace(data.BackgroundImage))
            imageOptions.Add(new BasicImage(data.BackgroundImage));

        if (!string.IsNullOrWhiteSpace(data.BackgroundImageAdditional))
            imageOptions.Add(new BasicImage(data.BackgroundImageAdditional));

        if (data.ShortScreenshots != null)
            imageOptions.AddRange(data.ShortScreenshots.Select(s => new BasicImage(s.Image)));

        return imageOptions;
    }
}
