using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace MobyGamesMetadata;

public class MobyGamesBulkGenreAssigner(IPlayniteAPI playniteApi, IBulkPropertyImportDataSource<MobyGamesGenreSetting> dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism)
    : BulkGamePropertyAssigner<MobyGamesGenreSetting, GamePropertyImportViewModel>(playniteApi.Database, new(playniteApi) { AllowEmptySearchQuery = true }, dataSource, platformUtility, new MobyGamesIdUtility(), ExternalDatabase.MobyGames,
                                                                                   maxDegreeOfParallelism)
{
    public override string MetadataProviderName => "MobyGames";

    protected override PropertyImportSetting GetPropertyImportSetting(MobyGamesGenreSetting searchItem, out string name)
    {
        name = string.IsNullOrWhiteSpace(searchItem.NameOverride) ? searchItem.Name : searchItem.NameOverride;
        return new PropertyImportSetting { ImportTarget = searchItem.ImportTarget };
    }
}
