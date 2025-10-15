// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
namespace EaLibrary.Models;

public class OwnedGamesData
{
    public Me me { get; set; }
}

public class Me
{
    public string id { get; set; }
    public OwnedGameListCursor ownedGameProducts { get; set; }
    public string __typename { get; set; }
}

public class OwnedGameListCursor
{
    public string next { get; set; }
    public int totalCount { get; set; }
    public OwnedGameProduct[] items { get; set; }
    public string __typename { get; set; }
}

public class OwnedGameProduct
{
    public string id { get; set; }
    public string originOfferId { get; set; }
    public string status { get; set; }
    public Product product { get; set; }
    public string __typename { get; set; }
}

public class Product
{
    public string id { get; set; }
    public string name { get; set; }
    public bool downloadable { get; set; }
    public string gameSlug { get; set; }
    public bool isUngatedTrial { get; set; }
    public LifecycleStatus[] lifecycleStatus { get; set; }
    public AvailableInSubscription[] availableInSubscription { get; set; }
    public TrialDetails trialDetails { get; set; }
    public BaseItem baseItem { get; set; }
    public GamePlatformDetails gamePlatformDetails { get; set; }
    public GameProductUser gameProductUser { get; set; }
    public PurchaseStatus purchaseStatus { get; set; }
    public DownloadPackageTypeDetails[] downloadPackageTypeDetails { get; set; }
    public string __typename { get; set; }
}

public class LifecycleStatus
{
    public string lifecycleType { get; set; }
    public string downloadDate { get; set; }
    public string playableStartDate { get; set; }
    public string playableEndDate { get; set; }
    public string acquisitionStartDate { get; set; }
    public string acquisitionEndDate { get; set; }
    public string revealDate { get; set; }
    public string sunsetDate { get; set; }
    public string __typename { get; set; }
}

public class AvailableInSubscription
{
    public string id { get; set; }
    public string slug { get; set; }
    public string __typename { get; set; }
}

public class TrialDetails
{
    public int? trialDurationHours { get; set; }
    public string trialType { get; set; }
    public string __typename { get; set; }
}

public class BaseItem
{
    public string id { get; set; }
    public string baseGameSlug { get; set; }
    public string gameType { get; set; }
    public string title { get; set; }
    public string prereleaseGameType { get; set; }
    public bool hasGameHubPage { get; set; }
    public bool isLauncher { get; set; }
    public string __typename { get; set; }
}

public class GamePlatformDetails
{
    public string gamePlatform { get; set; }
    public string __typename { get; set; }
}

public class GameProductUser
{
    public string[] ownershipMethods { get; set; }
    public string initialEntitlementDate { get; set; }
    public string entitlementId { get; set; }
    public GameProductUserTrial gameProductUserTrial { get; set; }
    public string status { get; set; }
    public string __typename { get; set; }
}

public class GameProductUserTrial
{
    public int? trialTimeRemainingSeconds { get; set; }
    public string __typename { get; set; }
}

public class PurchaseStatus
{
    public bool repurchasable { get; set; }
    public string __typename { get; set; }
}

public class DownloadPackageTypeDetails
{
    public string platform { get; set; }
    public string downloadPackageType { get; set; }
    public string __typename { get; set; }
}
