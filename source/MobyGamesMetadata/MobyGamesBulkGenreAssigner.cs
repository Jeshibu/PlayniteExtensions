using Barnite.Scrapers;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace MobyGamesMetadata
{
    public class MobyGamesBulkGenreAssigner : BulkGamePropertyAssigner<MobyGamesGenreSetting, GamePropertyImportViewModel>
    {
        public MobyGamesBulkGenreAssigner(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<MobyGamesGenreSetting, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism)
            : base(playniteAPI, dataSource, platformUtility, new MobyGamesIdUtility(), ExternalDatabase.MobyGames, maxDegreeOfParallelism)
        {
            AllowEmptySearchQuery = true;
        }

        public override string MetadataProviderName { get; } = "MobyGames";

        protected override string GetGameIdFromUrl(string url)
        {
            var dbId = DatabaseIdUtility.GetIdFromUrl(url);
            return dbId.Database == ExternalDatabase.MobyGames ? dbId.Id : null;
        }

        protected override PropertyImportSetting GetPropertyImportSetting(MobyGamesGenreSetting searchItem, out string name)
        {
            name = string.IsNullOrWhiteSpace(searchItem.NameOverride) ? searchItem.Name : searchItem.NameOverride;
            return new PropertyImportSetting { ImportTarget = searchItem.ImportTarget };
        }
    }
}