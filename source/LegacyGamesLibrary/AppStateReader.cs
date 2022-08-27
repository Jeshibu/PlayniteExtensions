using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyGamesLibrary
{
    public interface IAppStateReader
    {
        IEnumerable<AppStateGame> GetUserOwnedGames();
    }

    public class AppStateReader : IAppStateReader
    {
        private static readonly string DefaultAppStatePath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\legacy-games-launcher\app-state.json");
        private ILogger logger = LogManager.GetLogger();

        public string AppStatePath { get; }

        public AppStateReader(string appStatePath = null)
        {
            AppStatePath = appStatePath ?? DefaultAppStatePath;
        }

        private class AppStateRoot
        {
            public AppStateSiteData SiteData;
            public AppStateUser User;
        }

        private class AppStateSiteData
        {
            public List<AppStateBundle> Catalog;
        }

        private class AppStateBundle
        {
            public int Id;
            public string Name;
            public string Permalink;
            public string Description;
            public List<AppStateGame> Games;
        }

        private class AppStateUser
        {
            public AppStateUserProfile Profile;
        }

        private class AppStateUserProfile
        {
            public List<AppStateUserDownloadLicense> Downloads;
        }

        private class AppStateUserDownloadLicense
        {
            [JsonProperty("product_id")]
            public int ProductId;
        }

        public IEnumerable<AppStateGame> GetUserOwnedGames()
        {
            if (!File.Exists(AppStatePath))
            {
                logger.Info($"Legacy Games app state file not found in {AppStatePath}");
                return null;
            }

            var fileContents = File.ReadAllText(AppStatePath);
            var appState = JsonConvert.DeserializeObject<AppStateRoot>(fileContents);

            var downloads = appState?.User?.Profile?.Downloads;
            var catalog = appState?.SiteData?.Catalog;

            if (downloads == null || catalog == null)
            {
                if (downloads == null)
                    logger.Warn($"Missing downloads section in {AppStatePath}");

                if (catalog == null)
                    logger.Warn($"Missing catalog section in {AppStatePath}");

                return null;
            }

            var ownedBundleIds = downloads.Select(d => d.ProductId).ToHashSet();
            var ownedBundles = catalog.Where(b => ownedBundleIds.Contains(b.Id)).ToList();
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
                    if (gamesByInstallerId.ContainsKey(game.InstallerUUID))
                        continue;

                    gamesByInstallerId.Add(game.InstallerUUID, game);
                }
            }
            return gamesByInstallerId.Values;
        }
    }

    public class AppStateGame
    {
        [JsonProperty("game_name")]
        public string GameName;
        [JsonProperty("game_description")]
        public string GameDescription;
        [JsonProperty("game_coverart")]
        public string GameCoverArt;
        [JsonProperty("game_installed_size")]
        public string GameInstalledSize;
        [JsonProperty("installer_uuid")]
        public Guid InstallerUUID;
    }
}
