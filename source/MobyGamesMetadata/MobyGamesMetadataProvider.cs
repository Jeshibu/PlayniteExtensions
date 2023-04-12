using MobyGamesMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobyGamesMetadata
{
    public class MobyGamesMetadataProvider : GenericMetadataProvider<GameSearchResult>
    {
        private readonly MobyGamesMetadata plugin;

        public override List<MetadataField> AvailableFields => plugin.SupportedFields;

        public MobyGamesMetadataProvider(MetadataRequestOptions options, MobyGamesMetadata plugin, IGameSearchProvider<GameSearchResult> dataSource, IPlatformUtility platformUtility)
            :base(dataSource, options, plugin.PlayniteApi, platformUtility)
        {
            this.plugin = plugin;
        }
    }
}