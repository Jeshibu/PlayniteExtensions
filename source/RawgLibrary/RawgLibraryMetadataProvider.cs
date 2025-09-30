using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using Rawg.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RawgLibrary;

public class RawgLibraryMetadataProvider(RawgLibrarySettings settings, RawgApiClient client, string languageCode = "eng") : LibraryMetadataProvider
{
    private readonly ILogger logger = LogManager.GetLogger();

    public override GameMetadata GetMetadata(Game game)
    {
        var data = client.GetGame(game.GameId);

        if (data == null)
            return new();

        return ToGameMetadata(data, logger, languageCode, settings);
    }

    public static GameMetadata ToGameMetadata(RawgGameDetails data, ILogger logger, string languageCode, RawgLibrarySettings settings)
    {
        var gameMetadata = new GameMetadata
        {
            GameId = data.Id.ToString(),
            Name = RawgMetadataHelper.StripYear(data.Name),
            Description = data.Description,
            ReleaseDate = RawgMetadataHelper.ParseReleaseDate(data, logger),
            CriticScore = data.Metacritic,
            CommunityScore = RawgMetadataHelper.ParseUserScore(data.Rating),
            Platforms = data.Platforms.NullIfEmpty()?.Select(RawgMetadataHelper.GetPlatform).ToHashSet(),
            BackgroundImage = data.BackgroundImage != null ? new MetadataFile(data.BackgroundImage) : null,
            Tags = data.Tags.NullIfEmpty()?.Where(t => t.Language == languageCode).Select(t => new MetadataNameProperty(t.Name)).ToHashSet<MetadataProperty>(),
            Genres = data.Genres.NullIfEmpty()?.Select(g => new MetadataNameProperty(g.Name)).ToHashSet<MetadataProperty>(),
            Developers = data.Developers.NullIfEmpty()?.Select(d => new MetadataNameProperty(d.Name.TrimCompanyForms())).ToHashSet<MetadataProperty>(),
            Publishers = data.Publishers.NullIfEmpty()?.Select(p => new MetadataNameProperty(p.Name.TrimCompanyForms())).ToHashSet<MetadataProperty>(),
            Links = RawgMetadataHelper.GetLinks(data).NullIfEmpty()?.ToList(),
        };

        if (data.UserGame != null)
        {
            if (settings.RawgToPlayniteStatuses.TryGetValue(data.UserGame.Status, out Guid? statusId) && statusId.HasValue && statusId != Guid.Empty)
            {
                if (statusId == RawgMapping.DoNotImportId)
                    return null;
                else
                    gameMetadata.CompletionStatus = new MetadataIdProperty(statusId.Value);
            }

            if (data.UserRating != 0 && settings.RawgToPlayniteRatings.TryGetValue(data.UserRating, out int playniteRating))
                gameMetadata.UserScore = playniteRating;
        }

        return gameMetadata;
    }
}