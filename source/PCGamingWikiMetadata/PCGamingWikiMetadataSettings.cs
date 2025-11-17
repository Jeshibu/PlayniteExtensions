using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;

namespace PCGamingWikiMetadata;

public class PCGamingWikiMetadataSettings: BulkImportPluginSettings
{
    public bool AddTagPrefix { get; set => SetValue(ref field, value); } = false;
    public bool ImportTagEngine { get; set => SetValue(ref field, value); } = true;
    public bool ImportTagMonetization { get; set => SetValue(ref field, value); } = false;
    public bool ImportTagMicrotransactions { get; set => SetValue(ref field, value); } = false;
    public bool ImportTagPacing { get; set => SetValue(ref field, value); } = true;
    public bool ImportTagPerspectives { get; set => SetValue(ref field, value); } = true;
    public bool ImportTagControls { get; set => SetValue(ref field, value); } = true;
    public bool ImportTagVehicles { get; set => SetValue(ref field, value); } = true;
    public bool ImportTagThemes { get; set => SetValue(ref field, value); } = true;
    public bool ImportTagArtStyle { get; set => SetValue(ref field, value); } = true;
    public bool ImportTagNoCloudSaves { get; set => SetValue(ref field, value); } = true;
    public bool ImportXboxPlayAnywhere { get; set => SetValue(ref field, value); } = true;
    public bool ImportMultiplayerTypes { get; set => SetValue(ref field, value); } = false;
    public bool ImportFeatureHDR { get; set => SetValue(ref field, value); } = true;
    public bool ImportFeatureRayTracing { get; set => SetValue(ref field, value); } = true;
    public bool ImportFeatureFramerate60 { get; set => SetValue(ref field, value); } = false;
    public bool ImportFeatureFramerate120 { get; set => SetValue(ref field, value); } = false;
    public bool ImportFeatureUltrawide { get; set => SetValue(ref field, value); } = false;
    public bool ImportFeatureVR { get; set => SetValue(ref field, value); } = false;
    public bool ImportFeatureVRHTCVive { get; set => SetValue(ref field, value); } = true;
    public bool ImportFeatureVROculusRift { get; set => SetValue(ref field, value); } = true;
    public bool ImportFeatureVROSVR { get; set => SetValue(ref field, value); } = true;
    public bool ImportFeatureVRWMR { get; set => SetValue(ref field, value); } = true;
    public bool ImportFeatureVRvorpX { get; set => SetValue(ref field, value); } = false;
    public bool ImportFeaturePlayStationControllers { get; set => SetValue(ref field, value); } = false;
    public bool ImportFeaturePlayStationButtonPrompts { get; set => SetValue(ref field, value); } = false;
    public bool ImportFeatureLightBar { get; set => SetValue(ref field, value); } = false;
    public bool ImportFeatureAdaptiveTrigger { get; set => SetValue(ref field, value); } = false;
    public bool ImportFeatureHapticFeedback { get; set => SetValue(ref field, value); } = false;
    public string TagPrefixMonetization { get; set => SetValue(ref field, value); } = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixMonetization")}]";
    public string TagPrefixMicrotransactions { get; set => SetValue(ref field, value); } = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixMicrotransactions")}]";
    public string TagPrefixPacing { get; set => SetValue(ref field, value); } = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixPacing")}]";
    public string TagPrefixPerspectives { get; set => SetValue(ref field, value); } = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixPerspectives")}]";
    public string TagPrefixControls { get; set => SetValue(ref field, value); } = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixControls")}]";
    public string TagPrefixVehicles { get; set => SetValue(ref field, value); } = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixVehicles")}]";
    public string TagPrefixThemes { get; set => SetValue(ref field, value); } = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixThemes")}]";
    public string TagPrefixEngines { get; set => SetValue(ref field, value); } = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixEngines")}]";
    public string TagPrefixArtStyles { get; set => SetValue(ref field, value); } = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixArtStyles")}]";
    public bool ImportLinkOfficialSite { get; set => SetValue(ref field, value); } = true;
    public bool ImportLinkHowLongToBeat { get; set => SetValue(ref field, value); } = true;
    public bool ImportLinkIGDB { get; set => SetValue(ref field, value); } = true;
    public bool ImportLinkIsThereAnyDeal { get; set => SetValue(ref field, value); } = false;
    public bool ImportLinkProtonDB { get; set => SetValue(ref field, value); } = false;
    public bool ImportLinkSteamDB { get; set => SetValue(ref field, value); } = true;
    public bool ImportLinkStrategyWiki { get; set => SetValue(ref field, value); } = true;
    public bool ImportLinkWikipedia { get; set => SetValue(ref field, value); } = true;
    public bool ImportLinkNexusMods { get; set => SetValue(ref field, value); } = true;
    public bool ImportLinkMobyGames { get; set => SetValue(ref field, value); } = true;
    public bool ImportLinkWSGF { get; set => SetValue(ref field, value); } = true;
    public bool ImportLinkWineHQ { get; set => SetValue(ref field, value); } = false;
    public bool ImportLinkGOGDatabase { get; set => SetValue(ref field, value); } = true;
    public bool ShowTopPanelButton { get; set; } = true;
}

public class PCGamingWikiMetadataSettingsViewModel : PluginSettingsViewModel<PCGamingWikiMetadataSettings, PCGamingWikiMetadata>
{
    public PCGamingWikiMetadataSettingsViewModel(PCGamingWikiMetadata plugin) : base(plugin, plugin.PlayniteApi)
    {
        Settings = LoadSavedSettings() ?? new PCGamingWikiMetadataSettings();
    }
}
