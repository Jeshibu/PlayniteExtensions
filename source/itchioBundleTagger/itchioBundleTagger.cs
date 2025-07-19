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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace itchioBundleTagger;

public class itchioBundleTagger : GenericPlugin
{
    private static readonly ILogger logger = LogManager.GetLogger();

    public override Guid Id { get; } = Guid.Parse("fc4fa75e-6e99-4c02-8547-113747efbb82");

    private itchioBundleTaggerSettingsViewModel Settings { get; set; }
    private Guid ItchIoLibraryId { get; }
    private ICachedFile DatabaseFile { get; }
    private itchIoTranslator Translator { get; }

    public itchioBundleTagger(IPlayniteAPI api) : base(api)
    {
        Translator = new itchIoTranslator(api.ApplicationSettings.Language);
        Settings = new itchioBundleTaggerSettingsViewModel(this, Translator);
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
        if (Settings.Settings.RunOnLibraryUpdate)
            TagItchBundleGames(PlayniteApi.Database.Games);
    }

    public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
    {
        if (Settings.Settings.ShowInContextMenu && args.Games.Any(g => g.PluginId == ItchIoLibraryId))
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

    private readonly Dictionary<string, Tag> TagsCache = [];

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

        string computedTagName = Settings.Settings.UseTagPrefix ? $"{Settings.Settings.TagPrefix}{name}" : name;

        bool tagIdFromSettings = Settings.Settings.TagIds.TryGetValue(key, out Guid tagId);

        Tag tag = null;
        if (tagIdFromSettings)
            tag = PlayniteApi.Database.Tags.Get(tagId);

        if (tag != null)
            tag.Name = computedTagName; //rename in case of switched localization-name or prefix

        tag ??= PlayniteApi.Database.Tags.FirstOrDefault(t => t.Name == computedTagName);

        if (tag == null)
            PlayniteApi.Database.Tags.Add(tag = new Tag(computedTagName));

        TagsCache.Add(key, tag);
        Settings.Settings.TagIds[key] = tag.Id;
        return tag;
    }

    private bool AddTagToGame(Game game, Tag tag)
    {
        var tagIds = game.TagIds ??= [];

        if (!tagIds.Contains(tag.Id))
        {
            tagIds.Add(tag.Id);
            return true;
        }
        return false;
    }

    private bool AddTagToGame(Game game, string tagKey)
    {
        var tag = GetTag(tagKey);
        if (tag is null)
            return false;

        return AddTagToGame(game, tag);
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
                        bool gameUpdated = false;

                        if (progressActionArgs.CancelToken.IsCancellationRequested)
                            return;

                        if (game.GameId == null || !allData.TryGetValue(game.GameId, out var data))
                            continue;

                        var steamId = GetSteamStoreUrlId(data.Steam);

                        if (steamId != null)
                        {
                            if (Settings.Settings.AddAvailableOnSteamTag)
                                gameUpdated |= AddTagToGame(game, "steam");

                            if (Settings.Settings.AddSteamLink && !GameHasSteamStoreLink(game, steamId))
                            {
                                var links = game.Links != null ? new ObservableCollection<Link>(game.Links) : [];
                                links.Add(new Link("Steam", data.Steam));
                                game.Links = links; //adding to observablecollections on another thread throws exceptions, so just replace them
                                gameUpdated = true;
                            }
                        }

                        if (Settings.Settings.AddFreeTag && string.IsNullOrWhiteSpace(data.CurrentPrice))
                            gameUpdated |= AddTagToGame(game, "free");

                        foreach (var bundleKey in data.Bundles.Keys)
                            if (Settings.Settings.BundleSettings.FirstOrDefault(b => b.Key == bundleKey)?.IsChecked != false)
                                gameUpdated |= AddTagToGame(game, $"bundle-{bundleKey}");

                        if (gameUpdated)
                        {
                            game.Modified = DateTime.Now;
                            PlayniteApi.Database.Games.Update(game);
                        }

                        i++;
                        if (i % 10 == 0)
                            progressActionArgs.CurrentProgressValue = relevantGames.Count + i;
                    }
                }
                SavePluginSettings(Settings.Settings); //for updates to Settings.TagIds
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

    private readonly Regex SteamUrlRegex = new(@"https://store\.steampowered\.com/app/(?<id>[0-9]+)", RegexOptions.Compiled);
    private string GetSteamStoreUrlId(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = SteamUrlRegex.Match(url);
        if (!match.Success)
            return null;

        return match.Groups["id"].Value;
    }

    private bool GameHasSteamStoreLink(Game game, string steamId)
    {
        if (string.IsNullOrWhiteSpace(steamId))
            return true;

        if (game.Links == null || game.Links.Count == 0)
            return false;

        foreach (var link in game.Links)
            if (GetSteamStoreUrlId(link.Url) == steamId)
                return true;

        return false;
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