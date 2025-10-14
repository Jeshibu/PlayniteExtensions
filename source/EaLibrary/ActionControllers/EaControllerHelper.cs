using EaLibrary.Models;
using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Threading.Tasks;

namespace EaLibrary.ActionControllers;

public static class EaControllerHelper
{
    public static async Task<LegacyOffer> LaunchGame(Game game, ILogger logger, EaLibrary eaLibrary)
    {
        var legacyOffer = await eaLibrary.DataGatherer.GetLegacyOfferAsync(game.GameId);
        if (string.IsNullOrWhiteSpace(legacyOffer?.contentId))
        {
            logger.Warn($"No content ID found for game {game.GameId} ({game.Name})");
            eaLibrary.PlayniteApi.Notifications.Add($"ea-launch-{game.GameId}-failed", $"Failed to get content ID for {game.Name}", NotificationType.Error);
            return null;
        }
        
        logger.Info($"Starting EA content {legacyOffer.contentId} ({game.Name})");

        ProcessStarter.StartUrl("origin2://game/launch/?offerIds=" + legacyOffer.contentId);
        
        return legacyOffer;
    }
}
