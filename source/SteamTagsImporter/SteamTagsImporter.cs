using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SteamTagsImporter
{
    public class SteamTagsImporter : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly Func<ISteamAppIdUtility> getAppIdUtility;
        private readonly Func<ISteamTagScraper> getTagScraper;

        private SteamTagsImporterSettings _settings;
        public SteamTagsImporterSettings Settings
        {
            get { return _settings ?? (_settings = new SteamTagsImporterSettings(this)); }
            set { _settings = value; }
        }

        public override Guid Id { get; } = Guid.Parse("01b67948-33a1-42d5-bd39-e4e8a226d215");

        public SteamTagsImporter(IPlayniteAPI api)
            : this(api, () => new SteamAppIdUtility(), () => new SteamTagScraper())
        {
        }

        public SteamTagsImporter(IPlayniteAPI api, Func<ISteamAppIdUtility> getAppIdUtility, Func<ISteamTagScraper> getTagScraper)
            : base(api)
        {
            this.Settings = new SteamTagsImporterSettings(this);
            this.getAppIdUtility = getAppIdUtility;
            this.getTagScraper = getTagScraper;
            this.Properties = new GenericPluginProperties { HasSettings = true };
        }

        public override ISettings GetSettings(bool firstRunSettings = false)
        {
            return Settings ?? (Settings = new SteamTagsImporterSettings(this));
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SteamTagsImporterSettingsView();
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            return new GameMenuItem[] { new GameMenuItem { Description = "Import Steam tags", Action = x => SetTags(x.Games) } };
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (!Settings.AutomaticallyAddTagsToNewGames)
                return;

            List<Game> games;
            if (Settings.LastAutomaticTagUpdate == DateTime.MinValue)
                games = new List<Game>();
            else
                games = PlayniteApi.Database.Games.Where(g => g.Added > Settings.LastAutomaticTagUpdate).ToList();

            logger.Debug($"Library update: {games.Count} games");

            SetTags(games);

            Settings.LastAutomaticTagUpdate = DateTime.Now;
            SavePluginSettings(Settings);
        }

        public void SetTags(List<Game> games)
        {
            string baseStatus = "Applying Steam tags to games…";

            PlayniteApi.Dialogs.ActivateGlobalProgress(args =>
            {
                args.ProgressMaxValue = games.Count;

                logger.Debug($"Adding tags to {games.Count} games");

                var appIdUtility = getAppIdUtility();
                var tagScraper = getTagScraper();

                //get settings from this thread because ObservableCollection (for OkayTags) does not like being addressed from other threads
                var settings = new SteamTagsImporterSettings(this); //this deserializes the settings from storage

                if (settings.LimitTagsToFixedAmount)
                    logger.Debug($"Max tags per game: {settings.FixedTagCount}");

                using (PlayniteApi.Database.BufferedUpdate())
                {
                    foreach (var game in games)
                    {
                        if (args.CancelToken.IsCancellationRequested)
                            return;

                        logger.Debug($"Setting tags for {game.Name}");

                        int currentGameIndex = (int)args.CurrentProgressValue + 1;

                        args.Text = $"{baseStatus} {currentGameIndex}/{games.Count}\n\n{game.Name}";

                        try
                        {
                            if (settings.LimitTaggingToPcGames && !IsPcGame(game))
                            {
                                logger.Debug($"Skipped {game.Name} because it's not a PC game");
                                args.CurrentProgressValue = currentGameIndex;
                                continue;
                            }

                            string appId = appIdUtility.GetSteamGameId(game);
                            if (string.IsNullOrEmpty(appId))
                            {
                                logger.Debug($"Couldn't find app ID for game {game.Name}");
                                args.CurrentProgressValue = currentGameIndex;
                                continue;
                            }

                            var tags = tagScraper.GetTags(appId);

                            if (settings.LimitTagsToFixedAmount)
                                tags = tags.Take(settings.FixedTagCount);

                            bool tagsAdded = false;
                            foreach (var tag in tags)
                            {
                                tagsAdded |= AddTagToGame(settings, game, tag);
                            }

                            if (tagsAdded)
                            {
                                game.Modified = DateTime.Now;
                                PlayniteApi.Database.Games.Update(game);
                            }

                            args.CurrentProgressValue = currentGameIndex;
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Error setting Steam tags");
                        }
                    }
                }

                //sort newly added whitelist entries in with the rest
                var sortedWhitelist = settings.OkayTags.OrderBy(a => a);
                settings.OkayTags = new System.Collections.ObjectModel.ObservableCollection<string>(sortedWhitelist);

                SavePluginSettings(settings);
                Settings = null; //force re-deserialization in the main thread to prevent ObservableCollection from throwing yet another fit
            }, new GlobalProgressOptions(baseStatus, cancelable: true) { IsIndeterminate = false });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="game"></param>
        /// <param name="tagName"></param>
        /// <returns>true if the tag was added, false if not</returns>
        private bool AddTagToGame(SteamTagsImporterSettings settings, Game game, string tagName)
        {
            bool blacklisted = settings.BlacklistedTags.Contains(tagName);

            if (blacklisted)
                return false;

            var tagIds = game.TagIds ?? (game.TagIds = new List<Guid>());

            var tag = PlayniteApi.Database.Tags.FirstOrDefault(t => tagName.Equals(t.Name, StringComparison.InvariantCultureIgnoreCase));

            if (tag == null)
            {
                tag = new Tag(tagName);
                PlayniteApi.Database.Tags.Add(tag);
            }

            bool whitelisted = settings.OkayTags.Contains(tagName);

            if (!whitelisted) //add unknown tags to the whitelist
                settings.OkayTags.Add(tagName);

            if (!tagIds.Contains(tag.Id) && !blacklisted)
            {
                tagIds.Add(tag.Id);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsPcGame(Game game)
        {
            var platforms = game.Platforms;
            if (platforms == null || platforms.Count == 0)
                return true; //assume games are for PC if not specified

            foreach (var platform in platforms)
            {
                if (platform.SpecificationId != null && platform.SpecificationId.StartsWith("pc_"))
                    return true;
            }

            return false;
        }
    }
}