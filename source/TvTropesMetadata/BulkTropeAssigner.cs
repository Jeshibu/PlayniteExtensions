﻿using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TvTropesMetadata.Scraping;

namespace TvTropesMetadata
{
    public class BulkTropeAssigner : BulkGamePropertyAssigner<TvTropesSearchResult>
    {
        private readonly TvTropesMetadataSettings settings;

        public BulkTropeAssigner(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<TvTropesSearchResult, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, TvTropesMetadataSettings settings)
            : base(playniteAPI, dataSource, platformUtility, settings.MaxDegreeOfParallelism)
        {
            this.settings = settings;
        }

        public override string MetadataProviderName { get; } = "TV Tropes";

        protected override UserControl GetBulkPropertyImportView(Window window, GamePropertyImportViewModel viewModel)
        {
            return new GamePropertyImportView(window) { DataContext = viewModel };
        }

        protected override string GetGameIdFromUrl(string url)
        {
            var trimmed = url.TrimStart("https://tvtropes.org/pmwiki/pmwiki.php/");
            if (trimmed != url)
                return url;
            else
                return null;
        }

        protected override PropertyImportSetting GetPropertyImportSetting(TvTropesSearchResult searchItem, out string name)
        {
            name = searchItem.Title;
            return new PropertyImportSetting { ImportTarget = PropertyImportTarget.Tags, Prefix = settings.TropePrefix };
        }
    }
}