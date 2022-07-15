using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Controls;

namespace itchioBundleTagger
{
    public class itchioBundleTagger : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("fc4fa75e-6e99-4c02-8547-113747efbb82");

        private itchioBundleTaggerSettings Settings { get; set; }
        private Guid ItchIoLibraryId { get; }
        private ICachedFile DatabaseFile { get; }
        private itchIoTranslator Translator { get; }

        public itchioBundleTagger(IPlayniteAPI api) : base(api)
        {
            Translator = new itchIoTranslator(api.ApplicationSettings.Language);
            Settings = new itchioBundleTaggerSettings(this, Translator);
            Properties = new GenericPluginProperties()
            {
                HasSettings = true
            };
            ItchIoLibraryId = BuiltinExtensions.GetIdFromExtension(BuiltinExtension.ItchioLibrary);
            DatabaseFile = new CachedFileDownloader(
                onlinePath: "https://randombundlegame.com/games.json",
                localPath: Path.Combine(GetPluginUserDataPath(), "games.json"),
                maxCacheAge: TimeSpan.FromDays(180),
                encoding: Encoding.UTF8,
                packagedFallbackPath: Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "games.json"));
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunView)
        {
            return new itchioBundleTaggerSettingsView();
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (Settings.RunOnLibraryUpdate)
                TagItchBundleGames(PlayniteApi.Database.Games);
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            if (Settings.ShowInContextMenu && args.Games.Any(g => g.PluginId == ItchIoLibraryId))
                return new[] { new GameMenuItem { Description = Translator.ExecuteTagging, Action = TagItchBundleGames } };
            else
                return new GameMenuItem[0];
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem { Description = Translator.RefreshDatabase, Action = DownloadNewDataFile, MenuSection = "@" + Translator.ExtensionName };
            yield return new MainMenuItem { Description = Translator.ExecuteTaggingAll, Action = TagItchBundleGamesForEverything, MenuSection = "@" + Translator.ExtensionName };
        }

        public void DownloadNewDataFile(MainMenuItemActionArgs args)
        {
            DatabaseFile.RefreshCache();
            PlayniteApi.Dialogs.ShowMessage(Translator.DatabaseRefreshed);
        }

        public Dictionary<string, ItchIoGame> GetAllBundleGameData()
        {
            return Playnite.SDK.Data.Serialization.FromJson<Dictionary<string, ItchIoGame>>(DatabaseFile.GetFileContents());
        }

        private Dictionary<string, Tag> TagsCache = new Dictionary<string, Tag>();

        private Tag GetTag(string key)
        {
            if (TagsCache.TryGetValue(key, out Tag cachedTag))
                return cachedTag;

            var name = Translator.GetTagName(key);
            if (name == null)
            {
                logger.Warn($"Unknown tag: {key}");
                return null;
            }

            string computedTagName = Settings.UseTagPrefix ? $"{Settings.TagPrefix}{name}" : name;

            bool tagIdFromSettings = Settings.TagIds.TryGetValue(key, out Guid tagId);

            Tag tag = null;
            if (tagIdFromSettings)
                tag = PlayniteApi.Database.Tags.Get(tagId);

            if (tag != null)
                tag.Name = computedTagName; //rename in case of switched localization-name or prefix

            if (tag == null)
                tag = PlayniteApi.Database.Tags.FirstOrDefault(t => t.Name == computedTagName);

            if (tag == null)
            {
                tag = new Tag(computedTagName);
                PlayniteApi.Database.Tags.Add(tag);
            }
            TagsCache.Add(key, tag);
            Settings.TagIds[key] = tag.Id;
            return tag;
        }

        private void AddTagToGame(Game game, Tag tag)
        {
            var tagIds = game.TagIds ?? (game.TagIds = new List<Guid>());

            if (!tagIds.Contains(tag.Id))
                tagIds.Add(tag.Id);
        }

        private void AddTagToGame(Game game, string tagKey)
        {
            var tag = GetTag(tagKey);
            if (tag is null)
                return;

            AddTagToGame(game, tag);
        }

        private void TagItchBundleGamesForEverything(MainMenuItemActionArgs args)
        {
            TagItchBundleGames(PlayniteApi.Database.Games);
        }

        private void TagItchBundleGames(GameMenuItemActionArgs args)
        {
            TagItchBundleGames(args.Games);
        }

        private void TagItchBundleGames(ICollection<Game> games)
        {
            PlayniteApi.Dialogs.ActivateGlobalProgress(progressActionArgs =>
            {
                try
                {
                    var relevantGames = games.Where(g => g.PluginId == ItchIoLibraryId).ToList();

                    if (relevantGames.Count == 0)
                        return;

                    progressActionArgs.ProgressMaxValue = relevantGames.Count * 2;

                    var allData = GetAllBundleGameData();

                    if (progressActionArgs.CancelToken.IsCancellationRequested)
                        return;

                    progressActionArgs.CurrentProgressValue = relevantGames.Count;

                    TagsCache.Clear(); //per-run cache; tags can have been edited/deleted in the meantime

                    progressActionArgs.Text = Translator.ProgressTagging;

                    using (PlayniteApi.Database.BufferedUpdate())
                    {
                        int i = 0;
                        foreach (var game in relevantGames)
                        {
                            if (progressActionArgs.CancelToken.IsCancellationRequested)
                                return;

                            if (game.GameId == null || !allData.TryGetValue(game.GameId, out var data))
                                continue;

                            if (!string.IsNullOrWhiteSpace(data.Steam))
                            {
                                if (Settings.AddAvailableOnSteamTag)
                                    AddTagToGame(game, "steam");

                                if (Settings.AddSteamLink)
                                {
                                    List<Link> links = new List<Link>();
                                    if (game.Links != null)
                                        links = new List<Link>(game.Links);
                                    else
                                        links = new List<Link>();

                                    if (!links.Any(l => l.Url.StartsWith(data.Steam)))
                                    {
                                        links.Add(new Link("Steam", data.Steam));
                                        game.Links = new ObservableCollection<Link>(links); //adding to observablecollections on another thread throws exceptions, so just replace them
                                    }
                                }
                            }

                            if (Settings.AddFreeTag && string.IsNullOrWhiteSpace(data.CurrentPrice))
                                AddTagToGame(game, "free");

                            foreach (var bundleKey in data.Bundles.Keys)
                                AddTagToGame(game, $"bundle-{bundleKey}");

                            PlayniteApi.Database.Games.Update(game);
                            i++;
                            if (i % 10 == 0)
                                progressActionArgs.CurrentProgressValue = relevantGames.Count + i;
                        }
                    }
                    SavePluginSettings(Settings); //for updates to Settings.TagIds
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error while tagging itch.io bundles");
                    PlayniteApi.Notifications.Add(new NotificationMessage("itch.io bundle tagger error", Translator.ErrorDisplayMessage(ex), NotificationType.Error));
                }
                finally
                {
                    TagsCache.Clear();
                }
            }, new GlobalProgressOptions(Translator.ProgressStart) { Cancelable = true, IsIndeterminate = false });
        }
    }

    public class ItchIoGame
    {
        public string Id;
        public string Title;
        public string Steam;
        public string CurrentPrice;
        public Dictionary<string, string> Bundles;
    }
}