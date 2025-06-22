using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;

namespace PCGamingWikiMetadata;

public class PCGamingWikiMetadataSettings: BulkImportPluginSettings
{
    private bool addTagPrefix = false;
    public bool AddTagPrefix { get { return addTagPrefix; } set { SetValue(ref addTagPrefix, value); } }
    private bool importTagEngine = true;
    public bool ImportTagEngine { get { return importTagEngine; } set { SetValue(ref importTagEngine, value); } }
    private bool importTagMonetization = false;
    public bool ImportTagMonetization { get { return importTagMonetization; } set { SetValue(ref importTagMonetization, value); } }
    private bool importTagMicrotransactions = false;
    public bool ImportTagMicrotransactions { get { return importTagMicrotransactions; } set { SetValue(ref importTagMicrotransactions, value); } }
    private bool importTagPacing = true;
    public bool ImportTagPacing { get { return importTagPacing; } set { SetValue(ref importTagPacing, value); } }
    private bool importTagPerspectives = true;
    public bool ImportTagPerspectives { get { return importTagPerspectives; } set { SetValue(ref importTagPerspectives, value); } }
    private bool importTagControls = true;
    public bool ImportTagControls { get { return importTagControls; } set { SetValue(ref importTagControls, value); } }
    private bool importTagVehicles = true;
    public bool ImportTagVehicles { get { return importTagVehicles; } set { SetValue(ref importTagVehicles, value); } }
    private bool importTagThemes = true;
    public bool ImportTagThemes { get { return importTagThemes; } set { SetValue(ref importTagThemes, value); } }
    private bool importTagArtStyle = true;
    public bool ImportTagArtStyle { get { return importTagArtStyle; } set { SetValue(ref importTagArtStyle, value); } }
    private bool importTagNoCloudSaves = true;
    public bool ImportTagNoCloudSaves { get { return importTagNoCloudSaves; } set { SetValue(ref importTagNoCloudSaves, value); } }

    private bool importXboxPlayAnywhere = true;
    public bool ImportXboxPlayAnywhere { get { return importXboxPlayAnywhere; } set { SetValue(ref importXboxPlayAnywhere, value); } }

    private bool importMultiplayerTypes = false;
    public bool ImportMultiplayerTypes { get { return importMultiplayerTypes; } set { SetValue(ref importMultiplayerTypes, value); } }

    private bool importFeatureHDR = true;
    public bool ImportFeatureHDR { get { return importFeatureHDR; } set { SetValue(ref importFeatureHDR, value); } }
    private bool importFeatureRayTracing = true;
    public bool ImportFeatureRayTracing { get { return importFeatureRayTracing; } set { SetValue(ref importFeatureRayTracing, value); } }
    private bool importFeatureFramerate60 = false;
    public bool ImportFeatureFramerate60 { get { return importFeatureFramerate60; } set { SetValue(ref importFeatureFramerate60, value); } }
    private bool importFeatureFramerate120 = false;
    public bool ImportFeatureFramerate120 { get { return importFeatureFramerate120; } set { SetValue(ref importFeatureFramerate120, value); } }
    private bool importFeatureUltrawide = false;
    public bool ImportFeatureUltrawide { get { return importFeatureUltrawide; } set { SetValue(ref importFeatureUltrawide, value); } }
    private bool importFeatureVR = false;
    public bool ImportFeatureVR { get { return importFeatureVR; } set { SetValue(ref importFeatureVR, value); } }

    private bool importFeatureVRHTCVive = true;
    public bool ImportFeatureVRHTCVive { get { return importFeatureVRHTCVive; } set { SetValue(ref importFeatureVRHTCVive, value); } }

    private bool importFeatureVROculusRift = true;
    public bool ImportFeatureVROculusRift { get { return importFeatureVROculusRift; } set { SetValue(ref importFeatureVROculusRift, value); } }

    private bool importFeatureVROSVR = true;
    public bool ImportFeatureVROSVR { get { return importFeatureVROSVR; } set { SetValue(ref importFeatureVROSVR, value); } }

    private bool importFeatureVRWMR = true;
    public bool ImportFeatureVRWMR { get { return importFeatureVRWMR; } set { SetValue(ref importFeatureVRWMR, value); } }
    private bool importFeatureVRvorpX = false;
    public bool ImportFeatureVRvorpX { get { return importFeatureVRvorpX; } set { SetValue(ref importFeatureVRvorpX, value); } }

    private bool importFeaturePlayStationControllers = false;
    public bool ImportFeaturePlayStationControllers { get { return importFeaturePlayStationControllers; } set { SetValue(ref importFeaturePlayStationControllers, value); } }

    private bool importFeaturePlayStationButtonPrompts = false;
    public bool ImportFeaturePlayStationButtonPrompts { get { return importFeaturePlayStationButtonPrompts; } set { SetValue(ref importFeaturePlayStationButtonPrompts, value); } }

    private bool importFeatureLightBar = false;
    public bool ImportFeatureLightBar { get { return importFeatureLightBar; } set { SetValue(ref importFeatureLightBar, value); } }

    private bool importFeatureAdaptiveTrigger = false;
    public bool ImportFeatureAdaptiveTrigger { get { return importFeatureAdaptiveTrigger; } set { SetValue(ref importFeatureAdaptiveTrigger, value); } }

    private bool importFeatureHapticFeedback = false;
    public bool ImportFeatureHapticFeedback { get { return importFeatureHapticFeedback; } set { SetValue(ref importFeatureHapticFeedback, value); } }

    private string tagPrefixMonetization = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixMonetization")}]";
    public string TagPrefixMonetization { get { return tagPrefixMonetization; } set { SetValue(ref tagPrefixMonetization, value); } }
    private string tagPrefixMicrotransactions = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixMicrotransactions")}]";
    public string TagPrefixMicrotransactions { get { return tagPrefixMicrotransactions; } set { SetValue(ref tagPrefixMicrotransactions, value); } }
    private string tagPrefixPacing = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixPacing")}]";
    public string TagPrefixPacing { get { return tagPrefixPacing; } set { SetValue(ref tagPrefixPacing, value); } }
    private string tagPrefixPerspectives = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixPerspectives")}]";
    public string TagPrefixPerspectives { get { return tagPrefixPerspectives; } set { SetValue(ref tagPrefixPerspectives, value); } }
    private string tagPrefixControls = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixControls")}]";
    public string TagPrefixControls { get { return tagPrefixControls; } set { SetValue(ref tagPrefixControls, value); } }
    private string tagPrefixVehicles = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixVehicles")}]";
    public string TagPrefixVehicles { get { return tagPrefixVehicles; } set { SetValue(ref tagPrefixVehicles, value); } }
    private string tagPrefixThemes = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixThemes")}]";
    public string TagPrefixThemes { get { return tagPrefixThemes; } set { SetValue(ref tagPrefixThemes, value); } }
    private string tagPrefixEngines = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixEngines")}]";
    public string TagPrefixEngines { get { return tagPrefixEngines; } set { SetValue(ref tagPrefixEngines, value); } }
    private string tagPrefixArtStyles = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixArtStyles")}]";
    public string TagPrefixArtStyles { get { return tagPrefixArtStyles; } set { SetValue(ref tagPrefixArtStyles, value); } }

    public bool ImportLinkOfficialSite { get { return importLinkOfficialSite; } set { SetValue(ref importLinkOfficialSite, value); } }
    private bool importLinkOfficialSite = true;
    public bool ImportLinkHowLongToBeat { get { return importLinkHowLongToBeat; } set { SetValue(ref importLinkHowLongToBeat, value); } }
    private bool importLinkHowLongToBeat = true;
    public bool ImportLinkIGDB { get { return importLinkIGDB; } set { SetValue(ref importLinkIGDB, value); } }
    private bool importLinkIGDB = true;
    public bool ImportLinkIsThereAnyDeal { get { return importLinkIsThereAnyDeal; } set { SetValue(ref importLinkIsThereAnyDeal, value); } }
    private bool importLinkIsThereAnyDeal = false;
    public bool ImportLinkProtonDB { get { return importLinkProtonDB; } set { SetValue(ref importLinkProtonDB, value); } }
    private bool importLinkProtonDB = false;
    public bool ImportLinkSteamDB { get { return importLinkSteamDB; } set { SetValue(ref importLinkSteamDB, value); } }
    private bool importLinkSteamDB = true;
    public bool ImportLinkStrategyWiki { get { return importLinkStrategyWiki; } set { SetValue(ref importLinkStrategyWiki, value); } }
    private bool importLinkStrategyWiki = true;
    public bool ImportLinkWikipedia { get { return importLinkWikipedia; } set { SetValue(ref importLinkWikipedia, value); } }
    private bool importLinkWikipedia = true;
    public bool ImportLinkNexusMods { get { return importLinkNexusMods; } set { SetValue(ref importLinkNexusMods, value); } }
    private bool importLinkNexusMods = true;
    public bool ImportLinkMobyGames { get { return importLinkMobyGames; } set { SetValue(ref importLinkMobyGames, value); } }
    private bool importLinkMobyGames = true;
    public bool ImportLinkWSGF { get { return importLinkWSGF; } set { SetValue(ref importLinkWSGF, value); } }
    private bool importLinkWSGF = true;
    public bool ImportLinkWineHQ { get { return importLinkWineHQ; } set { SetValue(ref importLinkWineHQ, value); } }
    private bool importLinkWineHQ = false;
    public bool ImportLinkGOGDatabase { get { return importLinkGOGDatabase; } set { SetValue(ref importLinkGOGDatabase, value); } }
    private bool importLinkGOGDatabase = true;

    public bool ShowTopPanelButton { get; set; } = true;

}

public class PCGamingWikiMetadataSettingsViewModel : PluginSettingsViewModel<PCGamingWikiMetadataSettings, PCGamingWikiMetadata>
{
    private readonly PCGamingWikiMetadata plugin;

    public PCGamingWikiMetadataSettingsViewModel(PCGamingWikiMetadata plugin):base(plugin, plugin.PlayniteApi)
    {
        // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
        this.plugin = plugin;

        // Load saved settings.
        Settings = plugin.LoadPluginSettings<PCGamingWikiMetadataSettings>() ?? new PCGamingWikiMetadataSettings();
    }
}
