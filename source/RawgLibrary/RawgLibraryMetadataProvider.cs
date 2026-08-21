using Playnite.SDK;
using Playnite.SDK.Models;
using Rawg.Common;
using System;

namespace RawgLibrary;

public class RawgLibraryMetadataProvider(RawgLibrarySettings settings, RawgApiClient client) : LibraryMetadataProvider
{
    private readonly ILogger logger = LogManager.GetLogger();

    public override GameMetadata GetMetadata(Game game)
    {
        var data = client.GetGame(game.GameId);

        if (data == null)
            return new();

        return ToGameMetadata(data, logger, settings);
    }

    public static GameMetadata ToGameMetadata(RawgGameDetails data, ILogger logger, RawgLibrarySettings settings)
    {
        var gameMetadata = RawgMetadataHelper.ToGameMetadata(data, logger, settings);

        if (data.UserGame != null)
        {
            if (settings.RawgToPlayniteStatuses.TryGetValue(data.UserGame.Status, out Guid? statusId) && statusId.HasValue && statusId != Guid.Empty)
            {
                if (statusId == RawgMapping.DoNotImportId)
                    return null;

                gameMetadata.CompletionStatus = new MetadataIdProperty(statusId.Value);
            }

            if (data.UserRating != 0 && settings.RawgToPlayniteRatings.TryGetValue(data.UserRating, out int playniteRating))
                gameMetadata.UserScore = playniteRating;
        }

        return gameMetadata;
    }
}
