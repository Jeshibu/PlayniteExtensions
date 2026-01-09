using GiantBombMetadata.Api;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace GiantBombMetadata;


public class GiantBombBulkPropertyAssigner(IPlayniteAPI playniteApi, GiantBombMetadataSettings settings, IBulkPropertyImportDataSource<GiantBombSearchResultItem> dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism)
    : BulkGamePropertyAssigner<GiantBombSearchResultItem, GamePropertyImportViewModel>(playniteApi.Database, new(playniteApi), dataSource, platformUtility, new GiantBombIdUtility(), ExternalDatabase.GiantBomb, maxDegreeOfParallelism)
{
    public GiantBombMetadataSettings Settings { get; } = settings;

    public override string MetadataProviderName => "Giant Bomb";

    protected override PropertyImportSetting GetPropertyImportSetting(GiantBombSearchResultItem selectedItem, out string propName)
    {
        propName = selectedItem.Name.Trim();
        var output = selectedItem.ResourceType switch
        {
            "character" => Settings.Characters,
            "concept" => Settings.Concepts,
            "object" => Settings.Objects,
            "location" => Settings.Locations,
            "person" => Settings.People,
            "theme" => Settings.Themes,
            "genre" => Settings.Genres,
            "franchise" => Settings.Franchises,
            _ => null,
        };
        if (output == null)
            logger.Error($"Unknown resource type: {selectedItem.ResourceType}");

        return output;
    }
}
