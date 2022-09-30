using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using Rawg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace RawgLibrary
{
    public class RawgLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private RawgLibrarySettingsViewModel settings { get; set; }

        private RawgApiClient rawgApiClient = null;

        public override Guid Id { get; } = Guid.Parse("e894b739-2d6e-41ee-aed4-2ea898e29803");

        public override string Name { get; } = "RAWG";

        public IWebDownloader Downloader { get; } = new WebDownloader();


        public RawgLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new RawgLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = true
            };
        }

        private void OpenSettings()
        {
            base.OpenSettingsView();
        }

        private RawgApiClient GetApiClient()
        {
            if (rawgApiClient != null)
                return rawgApiClient;

            if (string.IsNullOrWhiteSpace(settings.Settings.ApiKey))
            {
                PlayniteApi.Notifications.Add(new NotificationMessage("rawg-library-no-apikey", "No API key set. Please set it in the RAWG Metadata extension settings. (click this notification)", NotificationType.Error, OpenSettings));
                return null;
            }

            return rawgApiClient ?? (rawgApiClient = new RawgApiClient(new WebDownloader(), settings.Settings.ApiKey));
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            List<GameMetadata> output = new List<GameMetadata>();

            try
            {
                var client = GetApiClient();

                if (client == null)
                    return output;

                if (settings.Settings.ImportUserLibrary)
                {
                    if (string.IsNullOrWhiteSpace(settings.Settings.Username))
                    {
                        PlayniteApi.Notifications.Add(new NotificationMessage("rawg-library-no-username", "No username set. Please set it in the RAWG Metadata extension settings. (click this notification)", NotificationType.Error, OpenSettings));
                        return output;
                    }

                    var userLibrary = client.GetUserLibrary(settings.Settings.Username);
                    output.AddRange(userLibrary?.Results?.Select(g => RawgLibraryMetadataProvider.ToGameMetadata(g, logger, settings.Settings.LanguageCode)));
                }

                foreach (var collectionSettings in settings.Settings.Collections)
                {
                    if (!collectionSettings.Import)
                        continue;

                    var collectionGames = client.GetCollectionGames(collectionSettings.Collection.Id.ToString());
                    output.AddRange(collectionGames?.Results?.Select(g => RawgLibraryMetadataProvider.ToGameMetadata(g, logger, settings.Settings.LanguageCode)));
                }
            }
            catch (Exception ex)
            {
                PlayniteApi.Notifications.Add(new NotificationMessage("rawg-library-error", "Error while importing RAWG library: " + ex.Message, NotificationType.Error));
            }

            return output;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new RawgLibrarySettingsView();
        }

        public override LibraryMetadataProvider GetMetadataDownloader()
        {
            return base.GetMetadataDownloader();
        }
    }
}