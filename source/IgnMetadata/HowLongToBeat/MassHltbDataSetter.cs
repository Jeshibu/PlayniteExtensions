using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IgnMetadata.HowLongToBeat;

public class MassHltbDataSetter(IPlayniteAPI playniteApi, IWebDownloader downloader, bool showResultDialog = true)
{
    private readonly ILogger _logger = LogManager.GetLogger();
    private readonly PlatformUtility _platformUtility = new(playniteApi);
    private readonly HltbDataWriter _hltbDataWriter = new(playniteApi.Paths.ExtensionsDataPath);
    private readonly IgnScraper _ignScraper = new(downloader);
    private readonly IgnIdUtility _ignIdUtility = new();
    private readonly HltbIdUtility _hltbIdUtility = new();
    private IgnGameSearchProvider GameSearchProvider => field ??= new(new(downloader), _platformUtility);

    public void SetHltbData(IReadOnlyCollection<Game> games)
    {
        if (games == null || games.Count == 0)
            return;

        int updatedCount = 0;
        var baseProgressText = $"Setting IGN HowLongToBeat data for {games.Count} games...";
        using var _ = playniteApi.Database.BufferedUpdate();
        playniteApi.Dialogs.ActivateGlobalProgress(a =>
        {
            a.ProgressMaxValue = games.Count;
            foreach (var game in games)
            {
                try
                {
                    var metadataProvider = new IgnMetadataProvider(GameSearchProvider, new(game, true), playniteApi, _platformUtility);
                    var hltbDataProvider = new HltbDataProvider(metadataProvider, game, _hltbDataWriter, _ignScraper);
                    if (hltbDataProvider.SetHltbData(out var foundData))
                        updatedCount++;

                    if (AddLink(foundData?.IgnUrl, "IGN", _ignIdUtility) | AddLink(foundData?.HltbUrl, "HowLongToBeat", _hltbIdUtility))
                    {
                        game.Modified = DateTime.Now;
                        playniteApi.Database.Games.Update(game);
                    }

                    a.CurrentProgressValue++;
                    a.Text = $"""
                              {baseProgressText}
                              {a.CurrentProgressValue}/{games.Count}: {game.Name}
                              """;
                    continue;

                    bool AddLink(string url, string linkName, SimpleWebsiteIdUtility idUtility)
                    {
                        var urlId = idUtility.GetIdFromUrl(url);
                        if (urlId == default)
                            return false;

                        if (idUtility.GetIdsFromGame(game).Contains(urlId))
                            return false;

                        Link newLink = new(linkName, url);
                        if (game.Links == null)
                            game.Links = [newLink];
                        else
                            game.Links = [..game.Links, newLink];

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to set HLTB data for {game.Name} ({game.Id})");
                }
            }
        }, new(baseProgressText, true) { IsIndeterminate = false });

        if (showResultDialog)
            playniteApi.Dialogs.ShowMessage($"""
                                             Added HowLongToBeat data from IGN to {updatedCount} games.
                                             Restart Playnite to see any new HLTB data.
                                             """);
    }
}
