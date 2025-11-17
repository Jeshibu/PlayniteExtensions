using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using SteamTagsImporter.BulkImport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Controls;

namespace SteamTagsImporter;

public class SteamTagsImporter : MetadataPlugin
{
    private static readonly ILogger logger = LogManager.GetLogger();
    private readonly Func<ISteamAppIdUtility> getAppIdUtility;
    private readonly Func<ISteamTagScraper> getTagScraper;
    private readonly IWebDownloader downloader = new WebDownloader();

    public SteamTagsImporterSettingsViewModel Settings
    {
        get => field ??= new SteamTagsImporterSettingsViewModel(this);
        set;
    }

    public override Guid Id { get; } = Guid.Parse("01b67948-33a1-42d5-bd39-e4e8a226d215");

    public override string Name { get; } = "Steam Tags";

    public override List<MetadataField> SupportedFields { get; } = [MetadataField.Tags];

    public SteamTagsImporter(IPlayniteAPI api)
        : this(api, null, null)
    {
    }

    private ISteamAppIdUtility GetDefaultSteamAppUtility()
    {
        var appListCache = new CachedFileDownloader("https://api.steampowered.com/ISteamApps/GetAppList/v2/",
                                                    Path.Combine(GetPluginUserDataPath(), "SteamAppList.json"),
                                                    TimeSpan.FromDays(3),
                                                    Encoding.UTF8);
        return new SteamAppIdUtility(appListCache);
    }

    public SteamTagsImporter(IPlayniteAPI api, Func<ISteamAppIdUtility> getAppIdUtility = null, Func<ISteamTagScraper> getTagScraper = null)
        : base(api)
    {
        this.Settings = new SteamTagsImporterSettingsViewModel(this);
        this.getAppIdUtility = getAppIdUtility ?? GetDefaultSteamAppUtility;
        this.getTagScraper = getTagScraper ?? (() => new SteamTagScraper());
        this.Properties = new MetadataPluginProperties { HasSettings = true };
    }

    public override ISettings GetSettings(bool firstRunSettings = false) => Settings;

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new SteamTagsImporterSettingsView();
    }

    public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
    {
        yield return new GameMenuItem { Description = "Import Steam tags", Action = x => SetTags(x.Games) };
    }

    public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
    {
        if (!Settings.Settings.AutomaticallyAddTagsToNewGames)
            return;

        List<Game> games;
        if (Settings.Settings.LastAutomaticTagUpdate == DateTime.MinValue)
            games = [];
        else
            games = PlayniteApi.Database.Games.Where(g => g.Added > Settings.Settings.LastAutomaticTagUpdate).ToList();

        logger.Debug($"Library update: {games.Count} games");

        SetTags(games);

        Settings.Settings.LastAutomaticTagUpdate = DateTime.Now;
        SavePluginSettings(Settings.Settings);
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
            var settings = new SteamTagsImporterSettingsViewModel(this); //this deserializes the settings from storage

            if (settings.Settings.LimitTagsToFixedAmount)
                logger.Debug($"Max tags per game: {settings.Settings.FixedTagCount}");

            bool newTagsAddedToSettings = false;
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
                        SteamTagsGetter tagsGetter = new(settings.Settings, appIdUtility, tagScraper);
                        var steamTags = tagsGetter.GetSteamTags(game, out bool newTagsAdded);
                        var tagNames = steamTags.Select(t => tagsGetter.GetFinalTagName(t.Name));

                        newTagsAddedToSettings |= newTagsAdded;

                        bool tagsAdded = false;
                        foreach (var tag in tagNames)
                        {
                            tagsAdded |= AddTagToGame(settings.Settings, game, tag);
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

            if (newTagsAddedToSettings)
            {
                //sort newly added whitelist entries in with the rest
                var sortedWhitelist = settings.Settings.OkayTags.Distinct().OrderBy(a => a);
                settings.Settings.OkayTags = new System.Collections.ObjectModel.ObservableCollection<string>(sortedWhitelist);
            }

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
        var tag = PlayniteApi.Database.Tags.FirstOrDefault(t => tagName.Equals(t.Name, StringComparison.InvariantCultureIgnoreCase));

        if (tag == null)
        {
            tag = new Tag(tagName);
            PlayniteApi.Database.Tags.Add(tag);
        }

        var tagIds = game.TagIds ??= [];

        if (!tagIds.Contains(tag.Id))
        {
            tagIds.Add(tag.Id);
            return true;
        }
        else
        {
            return false;
        }
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        return new SteamTagsMetadataProvider(new SteamTagsGetter(Settings.Settings, getAppIdUtility(), getTagScraper()), options, this);
    }

    public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
    {
        yield return new MainMenuItem { Description = "Import Steam game property", MenuSection = "@Steam Tags Importer", Action = a => ImportGameProperty() };
    }

    public override IEnumerable<TopPanelItem> GetTopPanelItems()
    {
        if (!Settings.Settings.ShowTopPanelButton)
            yield break;

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var iconPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "icon.png");
        yield return new TopPanelItem()
        {
            Icon = iconPath,
            Visible = true,
            Title = "Import Steam game property",
            Activated = ImportGameProperty
        };
    }

    public void ImportGameProperty()
    {
        var searchProvider = new SteamPropertySearchProvider(new SteamSearch(downloader, Settings.Settings));
        var importer = new SteamPropertyBulkImporter(PlayniteApi, searchProvider, new PlatformUtility(PlayniteApi), Settings.Settings);
        importer.ImportGameProperty();
    }
}
