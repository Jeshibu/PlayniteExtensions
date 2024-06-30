using GiantBombMetadata.Api;
using GiantBombMetadata.SearchProviders;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;

namespace GiantBombMetadata
{

    public class GiantBombBulkPropertyAssigner : BulkGamePropertyAssigner<GiantBombSearchResultItem, GamePropertyImportViewModel>
    {
        public GiantBombMetadataSettings Settings { get; }

        public override string MetadataProviderName => "Giant Bomb";

        public GiantBombBulkPropertyAssigner(IPlayniteAPI playniteAPI, GiantBombMetadataSettings settings, GiantBombGamePropertySearchProvider dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism)
            : base(playniteAPI, dataSource, platformUtility, maxDegreeOfParallelism)
        {
            Settings = settings;
        }

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
}
