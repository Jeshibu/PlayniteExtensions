namespace EaLibrary.Models;

public class LegacyOffersData
{
    public LegacyOffer[] legacyOffers { get; set; }
}

public class LegacyOffer
{
    public string offerId { get; set; }
    public string contentId { get; set; }
    public string basePlatform { get; set; }
    public string primaryMasterTitleId { get; set; }
    public int mdmProjectNumber { get; set; }
    public string achievementSetOverride { get; set; }
    public string gameLauncherURL { get; set; }
    public string gameLauncherURLClientID { get; set; }
    public string stagingKeyPath { get; set; }
    public string[] mdmTitleIds { get; set; }
    public string multiplayerId { get; set; }
    public string executePathOverride { get; set; }
    public string installationDirectory { get; set; }
    public string installCheckOverride { get; set; }
    public bool? monitorPlay { get; set; }
    public string displayName { get; set; }
    public string displayType { get; set; }
    public string igoBrowserDefaultUrl { get; set; }
    public string executeParameters { get; set; }
    public string[] softwareLocales { get; set; }
    public string dipManifestRelativePath { get; set; }
    public string metadataInstallLocation { get; set; }
    public string distributionSubType { get; set; }
    public Downloads[] downloads { get; set; }
    public string locale { get; set; }
    public bool greyMarketControls { get; set; }
    public bool isDownloadable { get; set; }
    public bool isPreviewDownload { get; set; }
    public string downloadStartDate { get; set; }
    public string releaseDate { get; set; }
    public object useEndDate { get; set; }
    public object subscriptionUnlockDate { get; set; }
    public object subscriptionUseEndDate { get; set; }
    public string softwarePlatform { get; set; }
    public string softwareId { get; set; }
    public string downloadPackageType { get; set; }
    public string installerPath { get; set; }
    public string processorArchitecture { get; set; }
    public string macBundleID { get; set; }
    public int? gameEditionTypeFacetKeyRankDesc { get; set; }
    public string appliedCountryCode { get; set; }
    public string cloudSaveConfigurationOverride { get; set; }
    public FirstParties[] firstParties { get; set; }
    public object[] suppressedOfferIds { get; set; }
}

public class Downloads
{
    public bool igoApiEnabled { get; set; }
    public string downloadType { get; set; }
    public string version { get; set; }
    public bool executeElevated { get; set; }
    public string buildReleaseVersion { get; set; }
    public string buildLiveDate { get; set; }
    public string buildMetaData { get; set; }
    public string gameVersion { get; set; }
    public bool treatUpdatesAsMandatory { get; set; }
    public bool enableDifferentialUpdate { get; set; }
}

public class FirstParties
{
    public string partner { get; set; }
    public string partnerId { get; set; }
    public string partnerIdType { get; set; }
}
