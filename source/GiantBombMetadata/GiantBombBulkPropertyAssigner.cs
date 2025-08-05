using GiantBombMetadata.Api;
using GiantBombMetadata.SearchProviders;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;

namespace GiantBombMetadata;


public class GiantBombBulkPropertyAssigner(IPlayniteAPI playniteAPI, GiantBombMetadataSettings settings, GiantBombGamePropertySearchProvider dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism) : BulkGamePropertyAssigner<GiantBombSearchResultItem, GamePropertyImportViewModel>(playniteAPI, dataSource, platformUtility, new GiantBombIdUtility(), ExternalDatabase.GiantBomb, maxDegreeOfParallelism)
{
    public GiantBombMetadataSettings Settings { get; } = settings;

    public override string MetadataProviderName => "Giant Bomb";

    protected override PropertyImportSetting GetPropertyImportSetting(GiantBombSearchResultItem selectedItem, out string propName)
    {
        propName = selectedItem.Name.Trim();
        switch (selectedItem.ResourceType)
        {
            case "character":
                return Settings.Characters;
            case "concept":
                return Settings.Concepts;
            case "object":
                return Settings.Objects;
            case "location":
                return Settings.Locations;
            case "person":
                return Settings.People;
            default:
                logger.Error($"Unknown resource type: {selectedItem.ResourceType}");
                return null;
        }
    }

    protected override string GetGameIdFromUrl(string url)
    {
        return GiantBombHelper.GetGiantBombGuidFromUrl(url);
    }
}
