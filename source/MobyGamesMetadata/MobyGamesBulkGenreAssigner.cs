using Barnite.Scrapers;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MobyGamesMetadata
{
    public class MobyGamesBulkGenreAssigner : BulkGamePropertyAssigner<MobyGamesGenreSetting>
    {
        public MobyGamesBulkGenreAssigner(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<MobyGamesGenreSetting, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility)
            : base(playniteAPI, dataSource, platformUtility)
        {
            AllowEmptySearchQuery = true;
        }

        public override string MetadataProviderName { get; } = "MobyGames";

        protected override UserControl GetBulkPropertyImportView(Window window, PlayniteExtensions.Metadata.Common.GamePropertyImportViewModel viewModel)
        {
            return new GamePropertyImportView(window) { DataContext = viewModel };
        }

        protected override string GetGameIdFromUrl(string url)
        {
            return MobyGamesHelper.GetMobyGameIdStringFromUrl(url);
        }

        protected override PropertyImportSetting GetPropertyImportSetting(MobyGamesGenreSetting searchItem, out string name)
        {
            name = string.IsNullOrWhiteSpace(searchItem.NameOverride) ? searchItem.Name : searchItem.NameOverride;
            return new PropertyImportSetting { ImportTarget = searchItem.ImportTarget };
        }
    }
}