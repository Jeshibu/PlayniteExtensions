using Barnite.Scrapers;
using MobyGamesMetadata.Api;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MobyGamesMetadata
{
    public class MobyGamesBulkGroupAssigner : BulkGamePropertyAssigner<SearchResult>
    {
        private readonly MobyGamesMetadataSettings settings;

        public MobyGamesBulkGroupAssigner(IPlayniteAPI playniteAPI, MobyGamesMetadataSettings settings, ISearchableDataSourceWithDetails<SearchResult, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism)
            : base(playniteAPI, dataSource, platformUtility, maxDegreeOfParallelism)
        {
            this.settings = settings;
        }

        public override string MetadataProviderName => "MobyGames";

        protected override UserControl GetBulkPropertyImportView(Window window, GamePropertyImportViewModel viewModel)
        {
            return new GamePropertyImportView(window) { DataContext = viewModel };
        }

        protected override string GetGameIdFromUrl(string url)
        {
            return MobyGamesHelper.GetMobyGameIdStringFromUrl(url);
        }

        protected override PropertyImportSetting GetPropertyImportSetting(SearchResult searchItem, out string propName)
        {
            var importTarget = MobyGamesHelper.GetGroupImportTarget(searchItem.Name, out propName);

            return new PropertyImportSetting { ImportTarget = importTarget };
        }
    }
}