using IgnMetadata.HowLongToBeat.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Linq;

namespace IgnMetadata.HowLongToBeat;

public class HltbDataProvider(IgnMetadataProvider metadataProvider, Game game, HltbDataWriter writer, IgnScraper scraper)
{
    private readonly ILogger _logger = LogManager.GetLogger();
    private readonly IgnIdUtility _idUtility = new();

    public bool SetHltbData(out IgnHltbDataModel foundData)
    {
        foundData = null;
        try
        {
            if (writer.HasHltbData(game))
                return false;

            string url = GetIgnGameUrl();
            if (url == null)
                return false;

            foundData = scraper.GetHltbData(url);
            return writer.WriteHltbData(foundData, game);
        }
        catch (Exception e)
        {
            _logger.Error(e, $"Failed to set HLTB data for {game.Name} ({game.Id})");
            return false;
        }
    }

    private string GetIgnGameUrl()
    {
        try
        {
            var dbId = _idUtility.GetIdsFromGame(game).FirstOrDefault(id => id.Database == ExternalDatabase.IGN);
            if (dbId.Id != null)
                return _idUtility.GetUrlFromId(dbId.Id);

            var foundGame = metadataProvider.GetSearchResultGame(new());

            if (foundGame?.Url == null)
                return null;

            return new Uri(new Uri("https://www.ign.com/"), foundGame.Url).AbsoluteUri;
        }
        catch (Exception e)
        {
            _logger.Error(e, $"Failed to get IGN url for {game.Name} ({game.Id})");
            return null;
        }
    }
}
