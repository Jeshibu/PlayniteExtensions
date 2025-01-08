using Playnite.SDK;
using System.Collections.Generic;
using System.ComponentModel;

namespace PCGamingWikiMetadata
{
    public class PCGamingWikiMetadataSettings : ISettings, INotifyPropertyChanged
    {
        private readonly PCGamingWikiMetadata plugin;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool addTagPrefix = false;
        public bool AddTagPrefix { get { return addTagPrefix; } set { addTagPrefix = value; ; NotifyPropertyChanged("AddTagPrefix"); } }
        private bool importTagEngine = true;
        public bool ImportTagEngine { get { return importTagEngine; } set { importTagEngine = value; ; NotifyPropertyChanged("ImportTagEngine"); } }
        private bool importTagMonetization = false;
        public bool ImportTagMonetization { get { return importTagMonetization; } set { importTagMonetization = value; ; NotifyPropertyChanged("ImportTagMonetization"); } }
        private bool importTagMicrotransactions = false;
        public bool ImportTagMicrotransactions { get { return importTagMicrotransactions; } set { importTagMicrotransactions = value; ; NotifyPropertyChanged("ImportTagMicrotransactions"); } }
        private bool importTagPacing = true;
        public bool ImportTagPacing { get { return importTagPacing; } set { importTagPacing = value; ; NotifyPropertyChanged("ImportTagPacing"); } }
        private bool importTagPerspectives = true;
        public bool ImportTagPerspectives { get { return importTagPerspectives; } set { importTagPerspectives = value; ; NotifyPropertyChanged("ImportTagPerspectives"); } }
        private bool importTagControls = true;
        public bool ImportTagControls { get { return importTagControls; } set { importTagControls = value; ; NotifyPropertyChanged("ImportTagControls"); } }
        private bool importTagVehicles = true;
        public bool ImportTagVehicles { get { return importTagVehicles; } set { importTagVehicles = value; ; NotifyPropertyChanged("ImportTagVehicles"); } }
        private bool importTagThemes = true;
        public bool ImportTagThemes { get { return importTagThemes; } set { importTagThemes = value; ; NotifyPropertyChanged("ImportTagThemes"); } }
        private bool importTagArtStyle = true;
        public bool ImportTagArtStyle { get { return importTagArtStyle; } set { importTagArtStyle = value; ; NotifyPropertyChanged("ImportTagArtStyle"); } }
        private bool importTagNoCloudSaves = true;
        public bool ImportTagNoCloudSaves { get { return importTagNoCloudSaves; } set { importTagNoCloudSaves = value; ; NotifyPropertyChanged("ImportTagNoCloudSaves"); } }

        private bool importXboxPlayAnywhere = true;
        public bool ImportXboxPlayAnywhere { get { return importXboxPlayAnywhere; } set { importXboxPlayAnywhere = value; ; NotifyPropertyChanged("ImportXboxPlayAnywhere"); } }

        private bool importMultiplayerTypes = false;
        public bool ImportMultiplayerTypes { get { return importMultiplayerTypes; } set { importMultiplayerTypes = value; ; NotifyPropertyChanged("ImportMultiplayerTypes"); } }

        private bool importFeatureHDR = true;
        public bool ImportFeatureHDR { get { return importFeatureHDR; } set { importFeatureHDR = value; ; NotifyPropertyChanged("ImportFeatureHDR"); } }
        private bool importFeatureRayTracing = true;
        public bool ImportFeatureRayTracing { get { return importFeatureRayTracing; } set { importFeatureRayTracing = value; ; NotifyPropertyChanged("ImportFeatureRayTracing"); } }
        private bool importFeatureFramerate60 = false;
        public bool ImportFeatureFramerate60 { get { return importFeatureFramerate60; } set { importFeatureFramerate60 = value; ; NotifyPropertyChanged("ImportFeatureFramerate60"); } }
        private bool importFeatureFramerate120 = false;
        public bool ImportFeatureFramerate120 { get { return importFeatureFramerate120; } set { importFeatureFramerate120 = value; ; NotifyPropertyChanged("ImportFeatureFramerate120"); } }
        private bool importFeatureUltrawide = false;
        public bool ImportFeatureUltrawide { get { return importFeatureUltrawide; } set { importFeatureUltrawide = value; ; NotifyPropertyChanged("ImportFeatureUltrawide"); } }
        private bool importFeatureVR = false;
        public bool ImportFeatureVR { get { return importFeatureVR; } set { importFeatureVR = value; ; NotifyPropertyChanged("ImportFeatureVR"); } }

        private bool importFeatureVRHTCVive = true;
        public bool ImportFeatureVRHTCVive { get { return importFeatureVRHTCVive; } set { importFeatureVRHTCVive = value; ; NotifyPropertyChanged("ImportFeatureVRHTCVive"); } }

        private bool importFeatureVROculusRift = true;
        public bool ImportFeatureVROculusRift { get { return importFeatureVROculusRift; } set { importFeatureVROculusRift = value; ; NotifyPropertyChanged("ImportFeatureVROculusRift"); } }

        private bool importFeatureVROSVR = true;
        public bool ImportFeatureVROSVR { get { return importFeatureVROSVR; } set { importFeatureVROSVR = value; ; NotifyPropertyChanged("ImportFeatureVROSVR"); } }

        private bool importFeatureVRWMR = true;
        public bool ImportFeatureVRWMR { get { return importFeatureVRWMR; } set { importFeatureVRWMR = value; ; NotifyPropertyChanged("ImportFeatureVRWMR"); } }
        private bool importFeatureVRvorpX = false;
        public bool ImportFeatureVRvorpX { get { return importFeatureVRvorpX; } set { importFeatureVRvorpX = value; ; NotifyPropertyChanged("ImportFeatureVRvorpX"); } }

        private bool importFeaturePlayStationControllers = false;
        public bool ImportFeaturePlayStationControllers { get { return importFeaturePlayStationControllers; } set { importFeaturePlayStationControllers = value; ; NotifyPropertyChanged(nameof(ImportFeaturePlayStationControllers)); } }

        private bool importFeaturePlayStationButtonPrompts = false;
        public bool ImportFeaturePlayStationButtonPrompts { get { return importFeaturePlayStationButtonPrompts; } set { importFeaturePlayStationButtonPrompts = value; ; NotifyPropertyChanged(nameof(ImportFeaturePlayStationButtonPrompts)); } }

        private bool importFeatureLightBar = false;
        public bool ImportFeatureLightBar { get { return importFeatureLightBar; } set { importFeatureLightBar = value; ; NotifyPropertyChanged(nameof(ImportFeatureLightBar)); } }

        private bool importFeatureAdaptiveTrigger = false;
        public bool ImportFeatureAdaptiveTrigger { get { return importFeatureAdaptiveTrigger; } set { importFeatureAdaptiveTrigger = value; ; NotifyPropertyChanged(nameof(ImportFeatureAdaptiveTrigger)); } }

        private bool importFeatureHapticFeedback = false;
        public bool ImportFeatureHapticFeedback { get { return importFeatureHapticFeedback; } set { importFeatureHapticFeedback = value; ; NotifyPropertyChanged(nameof(ImportFeatureHapticFeedback)); } }

        private string tagPrefixMonetization = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixMonetization")}]";
        public string TagPrefixMonetization { get { return tagPrefixMonetization; } set { tagPrefixMonetization = value; ; NotifyPropertyChanged("TagPrefixMonetization"); } }
        private string tagPrefixMicrotransactions = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixMicrotransactions")}]";
        public string TagPrefixMicrotransactions { get { return tagPrefixMicrotransactions; } set { tagPrefixMicrotransactions = value; ; NotifyPropertyChanged("TagPrefixMicrotransactions"); } }
        private string tagPrefixPacing = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixPacing")}]";
        public string TagPrefixPacing { get { return tagPrefixPacing; } set { tagPrefixPacing = value; ; NotifyPropertyChanged("TagPrefixPacing"); } }
        private string tagPrefixPerspectives = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixPerspectives")}]";
        public string TagPrefixPerspectives { get { return tagPrefixPerspectives; } set { tagPrefixPerspectives = value; ; NotifyPropertyChanged("TagPrefixPerspectives"); } }
        private string tagPrefixControls = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixControls")}]";
        public string TagPrefixControls { get { return tagPrefixControls; } set { tagPrefixControls = value; ; NotifyPropertyChanged("TagPrefixControls"); } }
        private string tagPrefixVehicles = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixVehicles")}]";
        public string TagPrefixVehicles { get { return tagPrefixVehicles; } set { tagPrefixVehicles = value; ; NotifyPropertyChanged("TagPrefixVehicles"); } }
        private string tagPrefixThemes = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixThemes")}]";
        public string TagPrefixThemes { get { return tagPrefixThemes; } set { tagPrefixThemes = value; ; NotifyPropertyChanged("TagPrefixThemes"); } }
        private string tagPrefixEngines = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixEngines")}]";
        public string TagPrefixEngines { get { return tagPrefixEngines; } set { tagPrefixEngines = value; ; NotifyPropertyChanged("TagPrefixEngines"); } }
        private string tagPrefixArtStyles = $"[{ResourceProvider.GetString("LOCPCGWSettingsTagPrefixArtStyles")}]";
        public string TagPrefixArtStyles { get { return tagPrefixArtStyles; } set { tagPrefixArtStyles = value; ; NotifyPropertyChanged("TagPrefixArtStyles"); } }

        public bool ImportLinkOfficialSite { get { return importLinkOfficialSite; } set { importLinkOfficialSite = value; ; NotifyPropertyChanged("ImportLinkOfficialSite"); } }
        private bool importLinkOfficialSite = true;
        public bool ImportLinkHowLongToBeat { get { return importLinkHowLongToBeat; } set { importLinkHowLongToBeat = value; ; NotifyPropertyChanged("ImportLinkHowLongToBeat"); } }
        private bool importLinkHowLongToBeat = true;
        public bool ImportLinkIGDB { get { return importLinkIGDB; } set { importLinkIGDB = value; ; NotifyPropertyChanged("ImportLinkIGDB"); } }
        private bool importLinkIGDB = true;
        public bool ImportLinkIsThereAnyDeal { get { return importLinkIsThereAnyDeal; } set { importLinkIsThereAnyDeal = value; ; NotifyPropertyChanged("ImportLinkIsThereAnyDeal"); } }
        private bool importLinkIsThereAnyDeal = false;
        public bool ImportLinkProtonDB { get { return importLinkProtonDB; } set { importLinkProtonDB = value; ; NotifyPropertyChanged("ImportLinkProtonDB"); } }
        private bool importLinkProtonDB = false;
        public bool ImportLinkSteamDB { get { return importLinkSteamDB; } set { importLinkSteamDB = value; ; NotifyPropertyChanged("ImportLinkSteamDB"); } }
        private bool importLinkSteamDB = true;
        public bool ImportLinkStrategyWiki { get { return importLinkStrategyWiki; } set { importLinkStrategyWiki = value; ; NotifyPropertyChanged("ImportLinkStrategyWiki"); } }
        private bool importLinkStrategyWiki = true;
        public bool ImportLinkWikipedia { get { return importLinkWikipedia; } set { importLinkWikipedia = value; ; NotifyPropertyChanged("ImportLinkWikipedia"); } }
        private bool importLinkWikipedia = true;
        public bool ImportLinkNexusMods { get { return importLinkNexusMods; } set { importLinkNexusMods = value; ; NotifyPropertyChanged("ImportLinkNexusMods"); } }
        private bool importLinkNexusMods = true;
        public bool ImportLinkMobyGames { get { return importLinkMobyGames; } set { importLinkMobyGames = value; ; NotifyPropertyChanged("ImportLinkMobyGames"); } }
        private bool importLinkMobyGames = true;
        public bool ImportLinkWSGF { get { return importLinkWSGF; } set { importLinkWSGF = value; ; NotifyPropertyChanged("ImportLinkWSGF"); } }
        private bool importLinkWSGF = true;
        public bool ImportLinkWineHQ { get { return importLinkWineHQ; } set { importLinkWineHQ = value; ; NotifyPropertyChanged("ImportLinkWineHQ"); } }
        private bool importLinkWineHQ = false;
        public bool ImportLinkGOGDatabase { get { return importLinkGOGDatabase; } set { importLinkGOGDatabase = value; ; NotifyPropertyChanged("ImportLinkGOGDatabase"); } }
        private bool importLinkGOGDatabase = true;

        // Parameterless constructor must exist if you want to use LoadPluginSettings method.
        public PCGamingWikiMetadataSettings()
        {
        }

        public PCGamingWikiMetadataSettings(PCGamingWikiMetadata plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<PCGamingWikiMetadataSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                ImportXboxPlayAnywhere = savedSettings.ImportXboxPlayAnywhere;
                ImportMultiplayerTypes = savedSettings.ImportMultiplayerTypes;

                AddTagPrefix = savedSettings.AddTagPrefix;
                ImportTagEngine = savedSettings.ImportTagEngine;
                ImportTagMonetization = savedSettings.ImportTagMonetization;
                ImportTagMicrotransactions = savedSettings.ImportTagMicrotransactions;
                ImportTagPacing = savedSettings.ImportTagPacing;
                ImportTagPerspectives = savedSettings.ImportTagPerspectives;
                ImportTagControls = savedSettings.ImportTagControls;
                ImportTagVehicles = savedSettings.ImportTagVehicles;
                ImportTagThemes = savedSettings.ImportTagThemes;
                ImportTagArtStyle = savedSettings.ImportTagArtStyle;
                ImportTagNoCloudSaves = savedSettings.ImportTagNoCloudSaves;

                ImportFeatureHDR = savedSettings.ImportFeatureHDR;
                ImportFeatureRayTracing = savedSettings.ImportFeatureRayTracing;
                ImportFeatureFramerate120 = savedSettings.ImportFeatureFramerate120;
                ImportFeatureFramerate60 = savedSettings.ImportFeatureFramerate60;
                ImportFeatureUltrawide = savedSettings.ImportFeatureUltrawide;

                ImportFeatureVR = savedSettings.importFeatureVR;
                ImportFeatureVRHTCVive = savedSettings.importFeatureVRHTCVive;
                ImportFeatureVROculusRift = savedSettings.importFeatureVROculusRift;
                ImportFeatureVROSVR = savedSettings.importFeatureVROSVR;
                ImportFeatureVRWMR = savedSettings.importFeatureVRWMR;

                ImportFeaturePlayStationControllers = savedSettings.importFeaturePlayStationControllers;
                ImportFeaturePlayStationButtonPrompts = savedSettings.ImportFeaturePlayStationButtonPrompts;
                ImportFeatureLightBar = savedSettings.ImportFeatureLightBar;
                ImportFeatureAdaptiveTrigger = savedSettings.importFeatureAdaptiveTrigger;
                ImportFeatureHapticFeedback = savedSettings.importFeatureHapticFeedback;

                TagPrefixMonetization = savedSettings.tagPrefixMonetization;
                TagPrefixMicrotransactions = savedSettings.tagPrefixMicrotransactions;
                TagPrefixPacing = savedSettings.tagPrefixPacing;
                TagPrefixPerspectives = savedSettings.tagPrefixPerspectives;
                TagPrefixControls = savedSettings.tagPrefixControls;
                TagPrefixVehicles = savedSettings.tagPrefixVehicles;
                TagPrefixThemes = savedSettings.tagPrefixThemes;
                TagPrefixEngines = savedSettings.tagPrefixEngines;
                TagPrefixArtStyles = savedSettings.tagPrefixArtStyles;

                ImportLinkOfficialSite = savedSettings.importLinkOfficialSite;
                ImportLinkHowLongToBeat = savedSettings.importLinkHowLongToBeat;
                ImportLinkIGDB = savedSettings.importLinkIGDB;
                ImportLinkIsThereAnyDeal = savedSettings.importLinkIsThereAnyDeal;
                ImportLinkProtonDB = savedSettings.importLinkProtonDB;
                ImportLinkSteamDB = savedSettings.importLinkSteamDB;
                ImportLinkStrategyWiki = savedSettings.importLinkStrategyWiki;
                ImportLinkWikipedia = savedSettings.importLinkWikipedia;
                ImportLinkNexusMods = savedSettings.importLinkNexusMods;
                ImportLinkMobyGames = savedSettings.importLinkMobyGames;
                ImportLinkWSGF = savedSettings.importLinkWSGF;
                ImportLinkWineHQ = savedSettings.importLinkWineHQ;
                ImportLinkGOGDatabase = savedSettings.importLinkGOGDatabase;
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(this);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}
