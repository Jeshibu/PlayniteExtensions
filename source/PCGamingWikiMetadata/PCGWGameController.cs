using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System;

namespace PCGamingWikiMetadata;

public class PCGWGameController
{
    private readonly ILogger logger = LogManager.GetLogger();
    public PcgwGame Game;
    private readonly PCGamingWikiMetadataSettings settings;

    public PCGamingWikiMetadataSettings Settings
    {
        get { return settings; }
    }

    private Dictionary<string, Func<bool>> settingsMap;
    private Dictionary<string, Action<string>> taxonomyFunctions;
    private Dictionary<string, Func<string>> taxonomyTagPrefix;

    public PCGWGameController(PCGamingWikiMetadataSettings settings)
    {
        this.settings = settings;
        InitalizeSettingsMappings();
    }

    public PCGWGameController(PcgwGame game, PCGamingWikiMetadataSettings settings)
    {
        Game = game;
        this.settings = settings;
        InitalizeSettingsMappings();
    }

    private void InitalizeSettingsMappings()
    {
        settingsMap = new Dictionary<string, Func<bool>>()
        {
            { PCGamingWikiType.Taxonomy.Engines, () => settings.ImportTagEngine },
            { PCGamingWikiType.Taxonomy.Monetization, () => settings.ImportTagMonetization },
            { PCGamingWikiType.Taxonomy.Microtransactions, () => settings.ImportTagMicrotransactions },
            { PCGamingWikiType.Taxonomy.Pacing, () => settings.ImportTagPacing },
            { PCGamingWikiType.Taxonomy.Perspectives, () => settings.ImportTagPerspectives },
            { PCGamingWikiType.Taxonomy.Controls, () => settings.ImportTagControls },
            { PCGamingWikiType.Taxonomy.Vehicles, () => settings.ImportTagVehicles },
            { PCGamingWikiType.Taxonomy.Themes, () => settings.ImportTagThemes },
            { PCGamingWikiType.Taxonomy.ArtStyles, () => settings.ImportTagArtStyle },
            { PCGamingWikiType.Taxonomy.Middleware, () => settings.ImportTagMiddleware },
            { PCGamingWikiType.Video.HDR, () => settings.ImportFeatureHDR },
            { PCGamingWikiType.Video.RayTracing, () => settings.ImportFeatureRayTracing },
            { PCGamingWikiType.Video.FPS120Plus, () => settings.ImportFeatureFramerate120 },
            { PCGamingWikiType.Video.FPS60, () => settings.ImportFeatureFramerate60 },
            { PCGamingWikiType.Video.Ultrawide, () => settings.ImportFeatureUltrawide },
            { PCGamingWikiType.Video.VR, () => settings.ImportFeatureVR },
            { PCGamingWikiType.VRHeadsets.HTCVive, () => settings.ImportFeatureVRHTCVive },
            { PCGamingWikiType.VRHeadsets.OculusRift, () => settings.ImportFeatureVROculusRift },
            { PCGamingWikiType.VRHeadsets.OSVR, () => settings.ImportFeatureVROSVR },
            { PCGamingWikiType.VRHeadsets.WindowsMixedReality, () => settings.ImportFeatureVRWMR },

            { PCGamingWikiType.Link.OfficialSite, () => settings.ImportLinkOfficialSite },
            { PCGamingWikiType.Link.HowLongToBeat, () => settings.ImportLinkHowLongToBeat },
            { PCGamingWikiType.Link.IGDB, () => settings.ImportLinkIGDB },
            { PCGamingWikiType.Link.IsThereAnyDeal, () => settings.ImportLinkIsThereAnyDeal },
            { PCGamingWikiType.Link.ProtonDB, () => settings.ImportLinkProtonDB },
            { PCGamingWikiType.Link.SteamDB, () => settings.ImportLinkSteamDB },
            { PCGamingWikiType.Link.StrategyWiki, () => settings.ImportLinkStrategyWiki },
            { PCGamingWikiType.Link.Wikipedia, () => settings.ImportLinkWikipedia },
            { PCGamingWikiType.Link.NexusMods, () => settings.ImportLinkNexusMods },
            { PCGamingWikiType.Link.MobyGames, () => settings.ImportLinkMobyGames },
            { PCGamingWikiType.Link.WSGF, () => settings.ImportLinkWSGF },
            { PCGamingWikiType.Link.WineHQ, () => settings.ImportLinkWineHQ },
            { PCGamingWikiType.Link.GOGDatabase, () => settings.ImportLinkGOGDatabase },
        };

        taxonomyTagPrefix = new Dictionary<string, Func<string>>()
        {
            { PCGamingWikiType.Taxonomy.Engines, () => settings.TagPrefixEngines },
            { PCGamingWikiType.Taxonomy.Themes, () => settings.TagPrefixThemes },
            { PCGamingWikiType.Taxonomy.ArtStyles, () => settings.TagPrefixArtStyles },
            { PCGamingWikiType.Taxonomy.Vehicles, () => settings.TagPrefixVehicles },
            { PCGamingWikiType.Taxonomy.Controls, () => settings.TagPrefixControls },
            { PCGamingWikiType.Taxonomy.Perspectives, () => settings.TagPrefixPerspectives },
            { PCGamingWikiType.Taxonomy.Pacing, () => settings.TagPrefixPacing },
            { PCGamingWikiType.Taxonomy.Monetization, () => settings.TagPrefixMonetization },
            { PCGamingWikiType.Taxonomy.Microtransactions, () => settings.TagPrefixMicrotransactions },
            { PCGamingWikiType.Taxonomy.Middleware, () => settings.TagPrefixMiddleware },
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
        return settings.AddTagPrefix
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

    private BuiltinExtension? LauncherNameToPluginId(string launcher) => launcher switch
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

        if (BuiltinExtensions.GetExtensionFromId(Game.LibraryGame.PluginId) == extension)
        {
            switch (description)
            {
                case PCGamingWikiType.Rating.NativeSupport:
                    Game.AddFeature("Cloud Saves");
                    break;
                case PCGamingWikiType.Rating.NotSupported:
                    if (settings.ImportTagNoCloudSaves)
                        Game.AddTag("No Cloud Saves");

                    break;
                case PCGamingWikiType.Rating.Unknown:
                    break;
            }
        }
    }

    public void AddVRFeature(string headset, string rating)
    {
        if (IsSettingDisabled(PCGamingWikiType.Video.VR))
        {
            return;
        }

        if (SettingExistsAndEnabled(headset) && NativeOrLimitedSupport(rating))
        {
            Game.AddVrFeature();
        }
    }

    public void AddVideoFeature(string key, string rating)
    {
        if (IsSettingDisabled(key) || !NativeOrLimitedSupport(rating))
        {
            return;
        }

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
        if (settings.ImportXboxPlayAnywhere)
        {
            Game.SetXboxPlayAnywhere();
        }
    }

    private bool NativeOrLimitedSupport(string rating)
    {
        return rating == PCGamingWikiType.Rating.NativeSupport ||
               rating == PCGamingWikiType.Rating.Limited;
    }

    public void AddMultiplayer(string networkType, string rating, short playerCount, IList<string> notes)
    {
        if (!settings.ImportMultiplayerTypes)
        {
            return;
        }

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
        {
            Game.Links.Add(link);
        }
    }

    public void AddMiddleware(string type, string name)
    {
        Game.AddMiddleware(type, name);
    }
}
