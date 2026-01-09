using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LegacyGamesLibrary;

public interface IAppStateReader
{
    IEnumerable<AppStateGame> GetUserOwnedGames();
}

public class AppStateReader(string appStatePath = null) : IAppStateReader
{
    private static readonly string DefaultAppStatePath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\legacy-games-launcher\app-state.json");
    private readonly ILogger logger = LogManager.GetLogger();

    public string AppStatePath { get; } = appStatePath ?? DefaultAppStatePath;

    public IEnumerable<AppStateGame> GetUserOwnedGames()
    {
        if (!File.Exists(AppStatePath))
        {
            logger.Info($"Legacy Games app state file not found in {AppStatePath}");
            return null;
        }

        var fileContents = File.ReadAllText(AppStatePath);
        var appState = JsonConvert.DeserializeObject<AppStateRoot>(fileContents);

        var user = appState?.User;
        var downloads = new List<AppStateUserDownloadLicense>();
        if (user?.GiveawayDownloads != null)
            downloads.AddRange(user.GiveawayDownloads);
        if (user?.Profile?.Downloads != null)
            downloads.AddRange(user.Profile.Downloads);

        var catalog = appState?.SiteData?.Catalog;

        if (downloads.Count == 0 || catalog == null)
        {
            if (catalog == null)
                logger.Warn($"Missing catalog section in {AppStatePath}");

            return null;
        }

        var ownedBundleIds = downloads.Select(d => d.ProductId).ToHashSet();

        var ownedBundles = new List<AppStateBundle>();
        foreach (int ownedBundleId in ownedBundleIds)
        {
            var bundle = catalog.FirstOrDefault(b => b.Id == ownedBundleId);
            if (bundle?.Games == null || bundle.Games.Count == 0)
            {
                logger.Info($"No catalog bundle found with games for {ownedBundleId}. Catalog entry: {bundle?.Name}");
                bundle = appState?.SiteData?.GiveawayCatalog?.FirstOrDefault(b => b.Id == ownedBundleId);
            }

            if (bundle == null)
                logger.Warn($"Could not find a bundle with ID {ownedBundleId}");
            else
                ownedBundles.Add(bundle);
        }

        var ownedGames = ownedBundles.SelectMany(b => b.Games);
        var gamesByInstallerId = new Dictionary<Guid, AppStateGame>();
        foreach (var bundle in ownedBundles)
        {
            if (bundle.Games == null)
            {
                logger.Warn($"No games for bundle {bundle.Id} - {bundle.Name}");
                continue;
            }

            foreach (var game in bundle.Games)
            {
                if (!Guid.TryParse(game.InstallerUUID, out Guid installerId))
                    continue;

                if (gamesByInstallerId.ContainsKey(installerId))
                    continue;

                gamesByInstallerId.Add(installerId, game);
            }
        }
        return gamesByInstallerId.Values;
    }
}
