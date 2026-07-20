using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System;

namespace PCGamingWikiMetadata;

public class PCGWGameController
{
    private readonly ILogger logger = LogManager.GetLogger();
    public PcgwGame Game;

    public PCGamingWikiMetadataSettings Settings { get; }

    private Dictionary<string, Func<bool>> settingsMap;
    private Dictionary<string, Action<string>> taxonomyFunctions;
    private Dictionary<string, Func<string>> taxonomyTagPrefix;

    public PCGWGameController(PCGamingWikiMetadataSettings settings)
    {
        this.Settings = settings;
        InitializeSettingsMappings();
    }

    public PCGWGameController(PcgwGame game, PCGamingWikiMetadataSettings settings)
    {
        Game = game;
        this.Settings = settings;
        InitializeSettingsMappings();
    }

    private void InitializeSettingsMappings()
    {
        settingsMap = new Dictionary<string, Func<bool>>()
        {
            { PCGamingWikiType.Taxonomy.Engines, () => Settings.ImportTagEngine },
            { PCGamingWikiType.Taxonomy.Monetization, () => Settings.ImportTagMonetization },
            { PCGamingWikiType.Taxonomy.Microtransactions, () => Settings.ImportTagMicrotransactions },
            { PCGamingWikiType.Taxonomy.Pacing, () => Settings.ImportTagPacing },
            { PCGamingWikiType.Taxonomy.Perspectives, () => Settings.ImportTagPerspectives },
            { PCGamingWikiType.Taxonomy.Controls, () => Settings.ImportTagControls },
            { PCGamingWikiType.Taxonomy.Vehicles, () => Settings.ImportTagVehicles },
            { PCGamingWikiType.Taxonomy.Themes, () => Settings.ImportTagThemes },
            { PCGamingWikiType.Taxonomy.ArtStyles, () => Settings.ImportTagArtStyle },
            { PCGamingWikiType.Taxonomy.Middleware, () => Settings.ImportTagMiddleware },
            { PCGamingWikiType.Video.HDR, () => Settings.ImportFeatureHDR },
            { PCGamingWikiType.Video.RayTracing, () => Settings.ImportFeatureRayTracing },
            { PCGamingWikiType.Video.FPS120Plus, () => Settings.ImportFeatureFramerate120 },
            { PCGamingWikiType.Video.FPS60, () => Settings.ImportFeatureFramerate60 },
            { PCGamingWikiType.Video.Ultrawide, () => Settings.ImportFeatureUltrawide },
            { PCGamingWikiType.Video.VR, () => Settings.ImportFeatureVR },
            { PCGamingWikiType.VRHeadsets.HTCVive, () => Settings.ImportFeatureVRHTCVive },
            { PCGamingWikiType.VRHeadsets.OculusRift, () => Settings.ImportFeatureVROculusRift },
            { PCGamingWikiType.VRHeadsets.OSVR, () => Settings.ImportFeatureVROSVR },
            { PCGamingWikiType.VRHeadsets.WindowsMixedReality, () => Settings.ImportFeatureVRWMR },

            { PCGamingWikiType.Link.OfficialSite, () => Settings.ImportLinkOfficialSite },
            { PCGamingWikiType.Link.HowLongToBeat, () => Settings.ImportLinkHowLongToBeat },
            { PCGamingWikiType.Link.IGDB, () => Settings.ImportLinkIGDB },
            { PCGamingWikiType.Link.IsThereAnyDeal, () => Settings.ImportLinkIsThereAnyDeal },
            { PCGamingWikiType.Link.ProtonDB, () => Settings.ImportLinkProtonDB },
            { PCGamingWikiType.Link.SteamDB, () => Settings.ImportLinkSteamDB },
            { PCGamingWikiType.Link.StrategyWiki, () => Settings.ImportLinkStrategyWiki },
            { PCGamingWikiType.Link.Wikipedia, () => Settings.ImportLinkWikipedia },
            { PCGamingWikiType.Link.NexusMods, () => Settings.ImportLinkNexusMods },
            { PCGamingWikiType.Link.MobyGames, () => Settings.ImportLinkMobyGames },
            { PCGamingWikiType.Link.WSGF, () => Settings.ImportLinkWSGF },
            { PCGamingWikiType.Link.WineHQ, () => Settings.ImportLinkWineHQ },
            { PCGamingWikiType.Link.GOGDatabase, () => Settings.ImportLinkGOGDatabase },
        };

        taxonomyTagPrefix = new Dictionary<string, Func<string>>()
        {
            { PCGamingWikiType.Taxonomy.Engines, () => Settings.TagPrefixEngines },
            { PCGamingWikiType.Taxonomy.Themes, () => Settings.TagPrefixThemes },
            { PCGamingWikiType.Taxonomy.ArtStyles, () => Settings.TagPrefixArtStyles },
            { PCGamingWikiType.Taxonomy.Vehicles, () => Settings.TagPrefixVehicles },
            { PCGamingWikiType.Taxonomy.Controls, () => Settings.TagPrefixControls },
            { PCGamingWikiType.Taxonomy.Perspectives, () => Settings.TagPrefixPerspectives },
            { PCGamingWikiType.Taxonomy.Pacing, () => Settings.TagPrefixPacing },
            { PCGamingWikiType.Taxonomy.Monetization, () => Settings.TagPrefixMonetization },
            { PCGamingWikiType.Taxonomy.Microtransactions, () => Settings.TagPrefixMicrotransactions },
            { PCGamingWikiType.Taxonomy.Middleware, () => Settings.TagPrefixMiddleware },
        };

        taxonomyFunctions = new Dictionary<string, Action<string>>()
        {
            { PCGamingWikiType.Taxonomy.Engines, value => Game.AddCsvTags(value, TagPrefix(PCGamingWikiType.Taxonomy.Engines)) },
            { PCGamingWikiType.Taxonomy.Themes, value => Game.AddCsvTags(value, TagPrefix(PCGamingWikiType.Taxonomy.Themes)) },
            { PCGamingWikiType.Taxonomy.ArtStyles, value => Game.AddCsvTags(value, TagPrefix(PCGamingWikiType.Taxonomy.ArtStyles)) },
            { PCGamingWikiType.Taxonomy.Vehicles, value => Game.AddCsvTags(value, TagPrefix(PCGamingWikiType.Taxonomy.Vehicles)) },
            { PCGamingWikiType.Taxonomy.Controls, value => Game.AddCsvTags(value, TagPrefix(PCGamingWikiType.Taxonomy.Controls)) },
            { PCGamingWikiType.Taxonomy.Perspectives, value => Game.AddCsvTags(value, TagPrefix(PCGamingWikiType.Taxonomy.Perspectives)) },
            { PCGamingWikiType.Taxonomy.Pacing, value => Game.AddCsvTags(value, TagPrefix(PCGamingWikiType.Taxonomy.Pacing)) },
            { PCGamingWikiType.Taxonomy.Monetization, value => Game.AddCsvTags(value, TagPrefix(PCGamingWikiType.Taxonomy.Monetization)) },
            { PCGamingWikiType.Taxonomy.Microtransactions, value => Game.AddCsvTags(value, TagPrefix(PCGamingWikiType.Taxonomy.Microtransactions)) },
            { PCGamingWikiType.Taxonomy.Modes, value => Game.AddCsvFeatures(value) },
            { PCGamingWikiType.Taxonomy.Genres, value => Game.AddGenres(value) },
            { PCGamingWikiType.Taxonomy.Series, value => Game.AddSeries(value) },
        };
    }

    private string TagPrefix(string taxonomyKey)
    {
        return Settings.AddTagPrefix
            ? taxonomyTagPrefix[taxonomyKey].Invoke()
            : "";
    }

    private bool SettingExistsAndEnabled(string key)
    {
        bool settingExists = settingsMap.TryGetValue(key, out Func<bool> enabled);
        return settingExists && enabled.Invoke();
    }

    private bool IsSettingDisabled(string key)
    {
        bool settingExists = settingsMap.TryGetValue(key, out Func<bool> enabled);
        return settingExists && !(enabled.Invoke());
    }

    public void AddTaxonomy(string key, string text)
    {
        if (IsSettingDisabled(key))
            return;

        if (text == PCGamingWikiType.TaxonomyValue.None)
            return;

        if (taxonomyFunctions.TryGetValue(key, out var action))
            action(text);
    }

    private static BuiltinExtension? LauncherNameToPluginId(string launcher) => launcher switch
    {
        PCGamingWikiType.Cloud.Steam => BuiltinExtension.SteamLibrary,
        PCGamingWikiType.Cloud.Xbox => BuiltinExtension.XboxLibrary,
        PCGamingWikiType.Cloud.GOG => BuiltinExtension.GogLibrary,
        PCGamingWikiType.Cloud.Epic => BuiltinExtension.EpicLibrary,
        PCGamingWikiType.Cloud.Ubisoft => BuiltinExtension.UplayLibrary,
        PCGamingWikiType.Cloud.Origin => BuiltinExtension.OriginLibrary,
        _ => null
    };

    public void AddCloudSaves(string launcher, string description)
    {
        BuiltinExtension? extension = LauncherNameToPluginId(launcher);

        if (BuiltinExtensions.GetExtensionFromId(Game.LibraryGame.PluginId) != extension)
            return;

        switch (description)
        {
            case PCGamingWikiType.Rating.NativeSupport:
                Game.AddFeature("Cloud Saves");
                break;
            case PCGamingWikiType.Rating.NotSupported:
                if (Settings.ImportTagNoCloudSaves)
                    Game.AddTag("No Cloud Saves");
                break;
            case PCGamingWikiType.Rating.Unknown:
                break;
        }
    }

    public void AddVRFeature(string headset, string rating)
    {
        if (IsSettingDisabled(PCGamingWikiType.Video.VR))
            return;

        if (SettingExistsAndEnabled(headset) && NativeOrLimitedSupport(rating))
            Game.AddVrFeature();
    }

    public void AddVideoFeature(string key, string rating)
    {
        if (IsSettingDisabled(key) || !NativeOrLimitedSupport(rating))
            return;

        switch (key)
        {
            case PCGamingWikiType.Video.HDR:
                Game.AddFeature("HDR");
                break;
            case PCGamingWikiType.Video.RayTracing:
                Game.AddFeature("Ray Tracing");
                break;
            case PCGamingWikiType.Video.FPS60:
                Game.SetFramerate60();
                break;
            case PCGamingWikiType.Video.FPS120Plus:
                Game.SetFramerate120Plus();
                break;
            case PCGamingWikiType.Video.Ultrawide:
                Game.AddFeature("Ultra-widescreen");
                break;
            case PCGamingWikiType.Video.FPS60And120:
                AddVideoFeature(PCGamingWikiType.Video.FPS60, rating);
                AddVideoFeature(PCGamingWikiType.Video.FPS120Plus, rating);
                break;
        }
    }

    public void SetXboxPlayAnywhere()
    {
        if (Settings.ImportXboxPlayAnywhere)
            Game.SetXboxPlayAnywhere();
    }

    private static bool NativeOrLimitedSupport(string rating) => rating is PCGamingWikiType.Rating.NativeSupport or PCGamingWikiType.Rating.Limited;

    public void AddMultiplayer(string networkType, string rating, short playerCount, IList<string> notes)
    {
        if (!Settings.ImportMultiplayerTypes)
            return;

        switch (networkType)
        {
            case PCGamingWikiType.Multiplayer.Local:
                Game.AddMultiplayerLocal(rating, playerCount, notes);
                break;
            case PCGamingWikiType.Multiplayer.LAN:
                Game.AddMultiplayerLan(rating, playerCount, notes);
                break;
            case PCGamingWikiType.Multiplayer.Online:
                Game.AddMultiplayerOnline(rating, playerCount, notes);
                break;
            case PCGamingWikiType.Multiplayer.Asynchronous:
                Game.AddMultiplayerAsynchronous(rating, playerCount, notes);
                break;
        }
    }

    public void AddDeveloper(string name)
    {
        Game.Developers.Add(new MetadataNameProperty(name));
    }

    public void AddPublisher(string name)
    {
        Game.Publishers.Add(new MetadataNameProperty(name));
    }

    public void AddLink(Link link)
    {
        if (SettingExistsAndEnabled(link.Name))
            Game.Links.Add(link);
    }

    public void AddMiddleware(string type, string name)
    {
        Game.AddMiddleware(type, name);
    }
}
