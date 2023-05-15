using Barnite.Scrapers;
using MobyGamesMetadata.Api;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
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

        protected override UserControl GetBulkPropertyImportView(Window window, PlayniteExtensions.Metadata.Common.GamePropertyImportViewModel viewModel)
        {
            return new GamePropertyImportView(window) { DataContext = viewModel };
        }

        protected override string GetGameIdFromUrl(string url)
        {
            return MobyGamesHelper.GetMobyGameIdStringFromUrl(url);
        }

        protected override PropertyImportSetting GetPropertyImportSetting(SearchResult searchItem, out string propName)
        {
            if (searchItem.Name.EndsWith(" series") && !searchItem.Name.EndsWith("TV series"))
            {
                propName = searchItem.Name.TrimEnd(" series");
                return new PropertyImportSetting { ImportTarget = PropertyImportTarget.Series };
            }
            if (searchItem.Name.StartsWith("Gameplay feature: "))
            {
                propName = searchItem.Name.TrimStart("Gameplay feature: ");
                return new PropertyImportSetting { ImportTarget = PropertyImportTarget.Features };
            };
            propName = searchItem.Name;
            return new PropertyImportSetting { ImportTarget = PropertyImportTarget.Tags };
        }
    }
}